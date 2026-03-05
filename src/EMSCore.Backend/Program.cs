using System.Reflection;
using Microsoft.EntityFrameworkCore;
using EMSCore.Infrastructure.Data;
using EMSCore.Infrastructure.Repositories;
using EMSCore.Infrastructure.Services;
using EMSCore.Domain.Interfaces;
using EMSCore.Application.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "EMS Core Backend API",
        Version = "v1",
        Description = "Energy Management System Backend API - Zentrale Datenverarbeitung und Systemverwaltung"
    });
    
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(EnergyMeasurementQueryHandlers).Assembly);
});

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
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddScoped<IEnergyMeasurementRepository, EnergyMeasurementRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();

builder.Services.Configure<MqttConfiguration>(
    builder.Configuration.GetSection("Mqtt"));

builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddScoped<EnergyDataMqttHandler>();

builder.Services.AddHostedService<BackendMqttBackgroundService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<EMSDbContext>()
    .AddCheck<BackendMqttHealthCheck>("mqtt");

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EMS Core Backend API v1");
        c.RoutePrefix = string.Empty;
    });
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        await context.Database.EnsureCreatedAsync();
        
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

app.Logger.LogInformation("EMS Core Backend System starting...");

app.Run();

/// <summary>
/// Background service that manages MQTT connection and subscribes to energy measurement topics.
/// Runs continuously to receive data from Edge systems.
/// </summary>
public class BackendMqttBackgroundService : BackgroundService
{
    private readonly IMqttService _mqttService;
    private readonly EnergyDataMqttHandler _energyHandler;
    private readonly ILogger<BackendMqttBackgroundService> _logger;

    public BackendMqttBackgroundService(
        IMqttService mqttService,
        EnergyDataMqttHandler energyHandler,
        ILogger<BackendMqttBackgroundService> logger)
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

            await _mqttService.StartAsync(stoppingToken);

            await _mqttService.SubscribeAsync("ems/+/devices/+/measurements/+", 
                _energyHandler.HandleEnergyMeasurementAsync, cancellationToken: stoppingToken);
            
            await _mqttService.SubscribeAsync("ems/+/devices/+/measurements/batch", 
                _energyHandler.HandleBatchMeasurementsAsync, cancellationToken: stoppingToken);
            
            await _mqttService.SubscribeAsync("ems/+/devices/+/status", 
                _energyHandler.HandleDeviceStatusAsync, cancellationToken: stoppingToken);

            _logger.LogInformation("MQTT subscriptions configured successfully");

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
/// Health check that verifies MQTT service connectivity.
/// Returns healthy if connected, unhealthy if disconnected.
/// </summary>
public class BackendMqttHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IMqttService _mqttService;

    public BackendMqttHealthCheck(IMqttService mqttService)
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
