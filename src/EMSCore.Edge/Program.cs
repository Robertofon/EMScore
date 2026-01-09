using System.Reflection;
using Microsoft.EntityFrameworkCore;
using EMSCore.Infrastructure.Data;
using EMSCore.Infrastructure.Repositories;
using EMSCore.Infrastructure.Services;
using EMSCore.Domain.Interfaces;
using EMSCore.Application.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "EMS Core Edge API", 
        Version = "v1",
        Description = "Energy Management System Edge API for device and measurement management"
    });
    
    // Include XML comments for better API documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add MediatR for CQRS
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(EnergyMeasurementQueryHandlers).Assembly);
});

// Add Entity Framework with PostgreSQL/TimescaleDB
builder.Services.AddDbContext<EMSDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Host=localhost;Database=emscore;Username=postgres;Password=postgres";
    
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    });
    
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Register repositories
builder.Services.AddScoped<IEnergyMeasurementRepository, EnergyMeasurementRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();

// Configure MQTT settings
builder.Services.Configure<MqttConfiguration>(
    builder.Configuration.GetSection("Mqtt"));

// Register MQTT service
builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddScoped<EnergyDataMqttHandler>();

// Add hosted service for MQTT
builder.Services.AddHostedService<MqttBackgroundService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContext<EMSDbContext>()
    .AddCheck<MqttHealthCheck>("mqtt");

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EMS Core Edge API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthorization();

app.MapControllers();

// Add health check endpoint
app.MapHealthChecks("/health");

// Ensure database is created and configured
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Ensure database exists
        await context.Database.EnsureCreatedAsync();
        
        // Configure TimescaleDB if not already done
        try
        {
            await context.ConfigureTimescaleDbAsync();
            logger.LogInformation("TimescaleDB configuration completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TimescaleDB configuration failed - this is normal if TimescaleDB extension is not available");
        }
        
        logger.LogInformation("Database initialization completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed");
    }
}

app.Logger.LogInformation("EMS Core Edge System starting...");

app.Run();

/// <summary>
/// Background service to manage MQTT connection lifecycle
/// </summary>
public class MqttBackgroundService : BackgroundService
{
    private readonly IMqttService _mqttService;
    private readonly EnergyDataMqttHandler _energyHandler;
    private readonly ILogger<MqttBackgroundService> _logger;

    public MqttBackgroundService(
        IMqttService mqttService,
        EnergyDataMqttHandler energyHandler,
        ILogger<MqttBackgroundService> logger)
    {
        _mqttService = mqttService;
        _energyHandler = energyHandler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting MQTT background service");

            // Start MQTT service
            await _mqttService.StartAsync(stoppingToken);

            // Subscribe to energy measurement topics
            await _mqttService.SubscribeAsync("ems/+/devices/+/measurements/+", 
                _energyHandler.HandleEnergyMeasurementAsync, cancellationToken: stoppingToken);
            
            await _mqttService.SubscribeAsync("ems/+/devices/+/measurements/batch", 
                _energyHandler.HandleBatchMeasurementsAsync, cancellationToken: stoppingToken);
            
            await _mqttService.SubscribeAsync("ems/+/devices/+/status", 
                _energyHandler.HandleDeviceStatusAsync, cancellationToken: stoppingToken);

            _logger.LogInformation("MQTT subscriptions configured successfully");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MQTT background service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MQTT background service");
        }
        finally
        {
            try
            {
                await _mqttService.StopAsync(CancellationToken.None);
                _logger.LogInformation("MQTT service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping MQTT service");
            }
        }
    }
}

/// <summary>
/// Health check for MQTT service
/// </summary>
public class MqttHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IMqttService _mqttService;

    public MqttHealthCheck(IMqttService mqttService)
    {
        _mqttService = mqttService;
    }

    public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = _mqttService.IsConnected;
            
            if (isConnected)
            {
                return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("MQTT service is connected"));
            }
            else
            {
                return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("MQTT service is not connected"));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("MQTT health check failed", ex));
        }
    }
}
