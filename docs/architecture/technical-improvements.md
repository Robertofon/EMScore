# EMS Core - Technische Verbesserungsempfehlungen

## Identifizierte Problembereiche in der aktuellen Spezifikation

### 1. **Datenbankspezifikation**
**Problem:** Vage "Zeitdatenerweiterung" ohne konkrete Technologie
**Lösung:** TimescaleDB als PostgreSQL-Extension

### 2. **Kommunikationsarchitektur**
**Problem:** Unklare Verwendung von MQTT vs. gRPC
**Lösung:** Hybride Architektur mit beiden Protokollen für verschiedene Anwendungsfälle

### 3. **Service Bus Implementierung**
**Problem:** Unklare Kombination von MediatR, Channels und Reactive Extensions
**Lösung:** Strukturierte Architektur mit klaren Verantwortlichkeiten

## Detaillierte technische Empfehlungen

### 1. **Datenbank-Architektur mit TimescaleDB und EF Core**

```csharp
// EF Core Entity für Zeitreihen-Daten
[Table("energy_measurements")]
public class EnergyMeasurement
{
    [Key]
    public long Id { get; set; }
    
    [Column("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [Column("device_id")]
    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;
    
    [Column("voltage")]
    public double Voltage { get; set; }
    
    [Column("current")]
    public double Current { get; set; }
    
    [Column("power")]
    public double Power { get; set; }
    
    [Column("frequency")]
    public double Frequency { get; set; }
    
    // Navigation Properties
    public virtual Device Device { get; set; } = null!;
}

// EF Core DbContext mit TimescaleDB
public class EMSDbContext : DbContext
{
    public DbSet<EnergyMeasurement> EnergyMeasurements { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TimescaleDB Hypertable-Konfiguration
        modelBuilder.Entity<EnergyMeasurement>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.DeviceId, e.Timestamp });
            
            // TimescaleDB-spezifische Konfiguration über Raw SQL
            entity.ToTable(tb => tb.HasComment("TimescaleDB Hypertable"));
        });
        
        // Lokale Benutzer und Rollen
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });
        
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId);
        });
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.UseTimescale(); // TimescaleDB Extension
        });
    }
}

// Repository Pattern mit EF Core
public interface IEnergyMeasurementRepository
{
    Task<IEnumerable<EnergyMeasurement>> GetMeasurementsAsync(string deviceId, DateTimeRange range);
    Task<EnergyMeasurement> AddMeasurementAsync(EnergyMeasurement measurement);
    Task<IEnumerable<EnergyMeasurement>> GetAggregatedDataAsync(string deviceId, TimeSpan interval);
}

public class EnergyMeasurementRepository : IEnergyMeasurementRepository
{
    private readonly EMSDbContext _context;
    
    public EnergyMeasurementRepository(EMSDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<EnergyMeasurement>> GetMeasurementsAsync(string deviceId, DateTimeRange range)
    {
        return await _context.EnergyMeasurements
            .Where(m => m.DeviceId == deviceId &&
                       m.Timestamp >= range.Start &&
                       m.Timestamp <= range.End)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }
}
```

**EF Core Vorteile:**
- Typsichere LINQ-Abfragen
- Automatische Migrations
- Change Tracking
- Lazy Loading Support
- TimescaleDB-spezifische Optimierungen

### 2. **Hybride Kommunikationsarchitektur**

#### MQTT für Sensor-Daten (Edge → Backend)
```csharp
public interface IMqttService
{
    Task PublishSensorDataAsync(string topic, SensorData data);
    Task SubscribeToDeviceUpdatesAsync(string devicePattern);
}

// Topic-Struktur
// ems/{site_id}/devices/{device_id}/measurements
// ems/{site_id}/devices/{device_id}/status
// ems/{site_id}/battery/soc
```

#### gRPC für Kommandos und Konfiguration (Backend → Edge)
```csharp
service EMSControlService {
    rpc UpdateDeviceConfiguration(DeviceConfigRequest) returns (DeviceConfigResponse);
    rpc ExecuteBatteryCommand(BatteryCommandRequest) returns (BatteryCommandResponse);
    rpc GetSystemStatus(SystemStatusRequest) returns (SystemStatusResponse);
}
```

**Verwendungsmatrix:**
- **MQTT:** Sensor-Daten, Telemetrie, Events (hohe Frequenz, fire-and-forget)
- **gRPC:** Konfiguration, Kommandos, Abfragen (Request-Response, Zuverlässigkeit)

### 3. **Service Bus Architektur**

```csharp
// MediatR für Command/Query Separation
public interface IEMSCommand : IRequest<Result> { }
public interface IEMSQuery<TResponse> : IRequest<TResponse> { }

// Channels für interne Datenströme
public class EnergyDataChannel
{
    private readonly Channel<EnergyMeasurement> _channel;
    
    public ChannelWriter<EnergyMeasurement> Writer => _channel.Writer;
    public ChannelReader<EnergyMeasurement> Reader => _channel.Reader;
}

// Reactive Extensions für Event-Streaming
public class EnergyEventStream
{
    private readonly Subject<EnergyEvent> _eventSubject = new();
    
    public IObservable<EnergyEvent> Events => _eventSubject.AsObservable();
    public void Publish(EnergyEvent energyEvent) => _eventSubject.OnNext(energyEvent);
}
```

### 4. **Plugin-Architektur**

```csharp
public interface IEMSModule
{
    string Name { get; }
    Version Version { get; }
    Task InitializeAsync(IServiceProvider serviceProvider);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public interface IBatteryModule : IEMSModule
{
    Task<BatteryStatus> GetStatusAsync();
    Task<Result> SetChargingModeAsync(ChargingMode mode);
    IObservable<BatteryEvent> BatteryEvents { get; }
}

// Module Discovery
[EMSModule("BatteryManagement", "1.0.0")]
public class LithiumBatteryModule : IBatteryModule
{
    // Implementation
}
```

### 5. **Konfigurationsmanagement**

```csharp
public class EMSConfiguration
{
    public DatabaseConfig Database { get; set; }
    public CommunicationConfig Communication { get; set; }
    public List<ModuleConfig> Modules { get; set; }
    public SecurityConfig Security { get; set; }
}

public class DatabaseConfig
{
    public string ConnectionString { get; set; }
    public TimeSpan DataRetentionPeriod { get; set; }
    public int CompressionAfterDays { get; set; }
}

public class CommunicationConfig
{
    public MqttConfig Mqtt { get; set; }
    public GrpcConfig Grpc { get; set; }
}
```

### 6. **Hybride Sicherheitsarchitektur mit EF Core und Keycloak**

```csharp
// Lokale Benutzer-Entitäten für EF Core
[Table("users")]
public class User
{
    [Key]
    public Guid Id { get; set; }
    
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    public bool IsEmergencyAccount { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation Properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

[Table("roles")]
public class Role
{
    [Key]
    public Guid Id { get; set; }
    
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public string Permissions { get; set; } = string.Empty; // JSON Array
    
    // Navigation Properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

[Table("user_roles")]
public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAt { get; set; }
    
    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}

// Hybride Authentifizierung Service
public interface IAuthenticationService
{
    Task<AuthResult> AuthenticateLocalAsync(string username, string password);
    Task<AuthResult> AuthenticateOidcAsync(string token);
    Task<AuthResult> AuthenticateDeviceAsync(string deviceId, string certificate);
    Task SyncUsersFromBackendAsync(); // Für Edge-Systeme
}

public class HybridAuthenticationService : IAuthenticationService
{
    private readonly EMSDbContext _context;
    private readonly IKeycloakService _keycloakService;
    private readonly IConfiguration _configuration;
    
    public async Task<AuthResult> AuthenticateLocalAsync(string username, string password)
    {
        // Lokale Authentifizierung für Notfälle und kleine Systeme
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            
        if (user == null || !VerifyPassword(password, user.PasswordHash))
            return AuthResult.Failed("Invalid credentials");
            
        var claims = BuildClaims(user);
        var token = GenerateJwtToken(claims);
        
        return AuthResult.Success(token, user);
    }
    
    public async Task<AuthResult> AuthenticateOidcAsync(string token)
    {
        // Keycloak/OIDC Authentifizierung für Vollausbau
        var tokenValidation = await _keycloakService.ValidateTokenAsync(token);
        if (!tokenValidation.IsValid)
            return AuthResult.Failed("Invalid OIDC token");
            
        // Benutzer aus Keycloak-Claims erstellen/aktualisieren
        var user = await SyncUserFromOidcAsync(tokenValidation.Claims);
        return AuthResult.Success(token, user);
    }
    
    public async Task SyncUsersFromBackendAsync()
    {
        // Edge-Systeme synchronisieren Benutzer vom Backend
        var backendUsers = await _backendApiService.GetUsersAsync();
        
        foreach (var backendUser in backendUsers)
        {
            var localUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == backendUser.Id);
            if (localUser == null)
            {
                _context.Users.Add(MapToLocalUser(backendUser));
            }
            else
            {
                UpdateLocalUser(localUser, backendUser);
            }
        }
        
        await _context.SaveChangesAsync();
    }
}

// Keycloak Integration
public interface IKeycloakService
{
    Task<TokenValidationResult> ValidateTokenAsync(string token);
    Task<IEnumerable<KeycloakUser>> GetUsersAsync();
    Task<KeycloakUser> CreateUserAsync(CreateUserRequest request);
    Task UpdateUserRolesAsync(string userId, IEnumerable<string> roles);
}

public class KeycloakService : IKeycloakService
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakConfiguration _config;
    
    public async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        var introspectionEndpoint = $"{_config.BaseUrl}/realms/{_config.Realm}/protocol/openid-connect/token/introspect";
        
        var response = await _httpClient.PostAsync(introspectionEndpoint, new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token", token),
            new KeyValuePair<string, string>("client_id", _config.ClientId),
            new KeyValuePair<string, string>("client_secret", _config.ClientSecret)
        }));
        
        var result = await response.Content.ReadFromJsonAsync<TokenIntrospectionResponse>();
        return new TokenValidationResult(result.Active, result.Claims);
    }
}

// Konfiguration für hybride Sicherheit
public class SecurityConfiguration
{
    public AuthenticationMode Mode { get; set; } = AuthenticationMode.Hybrid;
    public LocalAuthConfiguration Local { get; set; } = new();
    public KeycloakConfiguration Keycloak { get; set; } = new();
    public JwtConfiguration Jwt { get; set; } = new();
    public TlsConfiguration Tls { get; set; } = new();
}

public enum AuthenticationMode
{
    LocalOnly,      // Nur lokale Konten (Notfall/Kleinsysteme)
    OidcOnly,       // Nur Keycloak/OIDC (Vollausbau)
    Hybrid          // Beide Modi unterstützt
}

public class LocalAuthConfiguration
{
    public bool EnableEmergencyAccounts { get; set; } = true;
    public TimeSpan PasswordExpiry { get; set; } = TimeSpan.FromDays(90);
    public int MaxFailedAttempts { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
}

public class KeycloakConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = "ems";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool SyncUsers { get; set; } = true;
    public TimeSpan SyncInterval { get; set; } = TimeSpan.FromMinutes(15);
}

// Autorisierung mit lokalen und OIDC-Rollen
public class EMSAuthorizationHandler : AuthorizationHandler<EMSPermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EMSPermissionRequirement requirement)
    {
        var user = context.User;
        
        // Prüfe lokale Rollen
        if (user.HasClaim("role", requirement.Permission) ||
            user.HasClaim("local_role", requirement.Permission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        
        // Prüfe OIDC-Rollen
        if (user.HasClaim("realm_access", requirement.Permission) ||
            user.HasClaim("resource_access", requirement.Permission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        
        return Task.CompletedTask;
    }
}

// Benutzer-Synchronisation zwischen Backend und Edge
public class UserSyncService : BackgroundService
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<UserSyncService> _logger;
    private readonly SecurityConfiguration _config;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_config.Mode == AuthenticationMode.Hybrid ||
                    _config.Mode == AuthenticationMode.LocalOnly)
                {
                    await _authService.SyncUsersFromBackendAsync();
                }
                
                await Task.Delay(_config.Keycloak.SyncInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user synchronization");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
```

### 7. **Monitoring und Observability**

```csharp
// Structured Logging
public static class EMSLogEvents
{
    public static readonly EventId EnergyDataReceived = new(1001, "EnergyDataReceived");
    public static readonly EventId BatteryCommandExecuted = new(1002, "BatteryCommandExecuted");
    public static readonly EventId ModuleLoadFailed = new(2001, "ModuleLoadFailed");
}

// Metrics
public class EMSMetrics
{
    private readonly Counter _energyDataPoints;
    private readonly Histogram _commandExecutionTime;
    private readonly Gauge _activeBatteryModules;
}
```

## Nächste Schritte

1. **Architekturdiagramm erstellen** - Visualisierung der Komponenten und Datenflüsse
2. **API-Spezifikationen definieren** - OpenAPI/Swagger für REST, Proto-Dateien für gRPC
3. **Datenmodelle detaillieren** - Vollständige Entity-Definitionen
4. **Deployment-Strategien** - Docker, Kubernetes, Edge-Deployment
5. **Performance-Anforderungen** - Latenz, Durchsatz, Skalierbarkeit