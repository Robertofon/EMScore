# EMSCore - Verbesserte Spezifikation
## Energie-Management-System (EMS) mit Batterieunterstützung und erweiterbaren Modulen

### Projektbeschreibung

#### Ziel
Entwicklung eines hochskalierbaren Energie-Management-Systems (EMS) mit Batterieunterstützung und Plugin-basierter Erweiterbarkeit. Das System besteht aus einem zentralen Backend und verteilten Edge-Systemen, die über hybride Kommunikationsprotokolle (MQTT + gRPC) verbunden sind. Implementierung in .NET 10 mit TimescaleDB für Zeitreihendaten und einem reaktiven Service Bus.

#### Systemarchitektur

##### Hauptkomponenten
1. **Backend System:** Zentrale Datenverarbeitung, -speicherung und Systemverwaltung
2. **Edge Systems:** Dezentrale Datenerfassung und lokale Verarbeitung mit Offline-Fähigkeiten
3. **All-in-One Mode:** Vereinheitlichte Architektur ermöglicht Backend- und Edge-Funktionalität in einer Instanz

##### Technologie-Stack

###### Core Technologies
- **Framework:** .NET 10 mit ASP.NET Core
- **ORM:** Entity Framework Core 10 mit PostgreSQL Provider
- **Datenbank:** PostgreSQL 16+ mit TimescaleDB 2.14+ Extension
- **Kommunikation:**
  - MQTT 5.0 (Eclipse Mosquitto/HiveMQ) für Telemetriedaten
  - gRPC mit HTTP/2 für Kommandos und Konfiguration
- **Service Bus:** MediatR + System.Threading.Channels + Reactive Extensions (Rx.NET)
- **Authentifizierung:** Hybride Architektur (lokale Konten + Keycloak/OIDC)

###### Infrastructure
- **Containerization:** Docker mit Multi-Stage Builds
- **Orchestration:** Kubernetes (optional) oder Docker Compose
- **Monitoring:** OpenTelemetry + Prometheus + Grafana
- **Logging:** Serilog mit strukturiertem Logging

#### Detaillierte Funktionalitäten

##### 1. Datenverarbeitung und -speicherung mit EF Core

###### EF Core Entities
```csharp
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
    
    [Column("site_id")]
    [MaxLength(100)]
    public string SiteId { get; set; } = string.Empty;
    
    [Column("measurement_type")]
    public MeasurementType Type { get; set; }
    
    [Column("value")]
    public double Value { get; set; }
    
    [Column("unit")]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;
    
    [Column("quality_flag")]
    public QualityFlag Quality { get; set; } = QualityFlag.Good;
    
    [Column("metadata")]
    public string? Metadata { get; set; } // JSON String
    
    // Navigation Properties
    public virtual Device Device { get; set; } = null!;
    public virtual Site Site { get; set; } = null!;
}

[Table("devices")]
public class Device
{
    [Key]
    public string Id { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;
    
    public string SiteId { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation Properties
    public virtual Site Site { get; set; } = null!;
    public virtual ICollection<EnergyMeasurement> Measurements { get; set; } = new List<EnergyMeasurement>();
}

[Table("sites")]
public class Site
{
    [Key]
    public string Id { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Location { get; set; } = string.Empty;
    
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // Navigation Properties
    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
}

// EF Core DbContext mit TimescaleDB
public class EMSDbContext : DbContext
{
    public DbSet<EnergyMeasurement> EnergyMeasurements { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<Site> Sites { get; set; }
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
            entity.HasIndex(e => new { e.SiteId, e.Timestamp });
            
            // Relationships
            entity.HasOne(e => e.Device)
                  .WithMany(d => d.Measurements)
                  .HasForeignKey(e => e.DeviceId);
                  
            entity.HasOne(e => e.Site)
                  .WithMany()
                  .HasForeignKey(e => e.SiteId);
        });
        
        // Device Configuration
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasOne(d => d.Site)
                  .WithMany(s => s.Devices)
                  .HasForeignKey(d => d.SiteId);
        });
        
        // User Management
        ConfigureUserManagement(modelBuilder);
    }
    
    private void ConfigureUserManagement(ModelBuilder modelBuilder)
    {
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

public enum MeasurementType
{
    Voltage, Current, Power, Frequency, Temperature,
    BatterySOC, BatteryVoltage, SolarIrradiance
}

public enum QualityFlag
{
    Good = 0,
    Uncertain = 1,
    Bad = 2
}
```

###### Repository Pattern mit EF Core
```csharp
public interface IEnergyMeasurementRepository
{
    Task<IEnumerable<EnergyMeasurement>> GetMeasurementsAsync(string deviceId, DateTimeRange range);
    Task<EnergyMeasurement> AddMeasurementAsync(EnergyMeasurement measurement);
    Task<IEnumerable<EnergyMeasurement>> GetAggregatedDataAsync(string deviceId, TimeSpan interval);
    Task<IEnumerable<EnergyMeasurement>> GetSiteDataAsync(string siteId, DateTimeRange range);
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
    
    public async Task<IEnumerable<EnergyMeasurement>> GetAggregatedDataAsync(string deviceId, TimeSpan interval)
    {
        // TimescaleDB time_bucket Funktion über Raw SQL
        var sql = @"
            SELECT
                time_bucket(@interval, timestamp) as timestamp,
                device_id,
                measurement_type,
                AVG(value) as value,
                'avg' as unit,
                0 as quality_flag
            FROM energy_measurements
            WHERE device_id = @deviceId
            GROUP BY time_bucket(@interval, timestamp), device_id, measurement_type
            ORDER BY timestamp";
            
        return await _context.EnergyMeasurements
            .FromSqlRaw(sql, interval, deviceId)
            .ToListAsync();
    }
}
```

##### 2. Hybride Kommunikationsarchitektur

###### MQTT Implementation
```csharp
public interface IMqttService
{
    Task PublishAsync<T>(string topic, T payload, QualityOfService qos = QualityOfService.AtLeastOnce);
    Task SubscribeAsync(string topicFilter, Func<MqttMessage, Task> handler);
    Task<bool> IsConnectedAsync();
}

// Topic-Struktur
// Telemetrie: ems/{site_id}/devices/{device_id}/measurements/{type}
// Status: ems/{site_id}/devices/{device_id}/status
// Alerts: ems/{site_id}/alerts/{severity}
// Battery: ems/{site_id}/battery/{battery_id}/{command}
```

###### gRPC Services
```csharp
service EMSControlService {
    // Gerätekonfiguration
    rpc UpdateDeviceConfiguration(DeviceConfigRequest) returns (DeviceConfigResponse);
    rpc GetDeviceConfiguration(DeviceConfigRequest) returns (DeviceConfigResponse);
    
    // Batterie-Kommandos
    rpc ExecuteBatteryCommand(BatteryCommandRequest) returns (BatteryCommandResponse);
    rpc GetBatteryStatus(BatteryStatusRequest) returns (BatteryStatusResponse);
    
    // System-Management
    rpc GetSystemHealth(HealthCheckRequest) returns (HealthCheckResponse);
    rpc UpdateSystemConfiguration(SystemConfigRequest) returns (SystemConfigResponse);
    
    // Streaming für Echtzeitdaten
    rpc StreamEnergyData(StreamRequest) returns (stream EnergyDataResponse);
}
```

##### 3. Reaktiver Service Bus

###### MediatR Integration
```csharp
// Commands
public record UpdateBatteryModeCommand(string BatteryId, BatteryMode Mode) : IRequest<Result>;
public record StoreMeasurementCommand(EnergyMeasurement Measurement) : IRequest<Result>;

// Queries
public record GetEnergyDataQuery(string DeviceId, DateTimeRange Range) : IRequest<IEnumerable<EnergyMeasurement>>;
public record GetBatteryStatusQuery(string BatteryId) : IRequest<BatteryStatus>;

// Events
public record BatteryModeChangedEvent(string BatteryId, BatteryMode OldMode, BatteryMode NewMode);
public record EnergyThresholdExceededEvent(string DeviceId, double Threshold, double ActualValue);
```

###### Channel-basierte Datenströme
```csharp
public class EnergyDataPipeline
{
    private readonly Channel<EnergyMeasurement> _rawDataChannel;
    private readonly Channel<EnergyMeasurement> _processedDataChannel;
    
    public async Task ProcessDataStreamAsync(CancellationToken cancellationToken)
    {
        await foreach (var measurement in _rawDataChannel.Reader.ReadAllAsync(cancellationToken))
        {
            var processed = await ProcessMeasurementAsync(measurement);
            await _processedDataChannel.Writer.WriteAsync(processed, cancellationToken);
        }
    }
}
```

###### Reactive Extensions für Events
```csharp
public class EnergyEventStream
{
    private readonly Subject<EnergyEvent> _eventSubject = new();
    
    public IObservable<EnergyEvent> Events => _eventSubject.AsObservable();
    
    public IObservable<EnergyEvent> BatteryEvents => 
        Events.Where(e => e.EventType.StartsWith("Battery"));
    
    public IObservable<EnergyEvent> CriticalAlerts => 
        Events.Where(e => e.Severity >= AlertSeverity.Critical);
}
```

##### 4. Plugin-Architektur

###### Plugin Interface
```csharp
public interface IEMSModule
{
    string Name { get; }
    Version Version { get; }
    string Description { get; }
    IEnumerable<string> Dependencies { get; }
    
    Task<ModuleInitResult> InitializeAsync(IServiceProvider serviceProvider);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task<ModuleHealthStatus> GetHealthAsync();
}

[AttributeUsage(AttributeTargets.Class)]
public class EMSModuleAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; }
    public string Category { get; }
    
    public EMSModuleAttribute(string name, string version, string category = "General")
    {
        Name = name;
        Version = version;
        Category = category;
    }
}
```

###### Spezialisierte Module
```csharp
public interface IBatteryModule : IEMSModule
{
    Task<BatteryStatus> GetStatusAsync(string batteryId);
    Task<Result> SetChargingModeAsync(string batteryId, ChargingMode mode);
    Task<Result> SetDischargingLimitAsync(string batteryId, double limit);
    IObservable<BatteryEvent> BatteryEvents { get; }
}

public interface ISensorModule : IEMSModule
{
    Task<IEnumerable<SensorReading>> ReadSensorsAsync();
    IObservable<SensorReading> SensorStream { get; }
    Task<Result> CalibrateSensorAsync(string sensorId);
}

public interface IAnalyticsModule : IEMSModule
{
    Task<EnergyForecast> GenerateForecastAsync(ForecastRequest request);
    Task<OptimizationResult> OptimizeEnergyUsageAsync(OptimizationRequest request);
    IObservable<AnalyticsInsight> Insights { get; }
}
```

##### 5. Konfigurationsmanagement

```csharp
public class EMSConfiguration
{
    public DatabaseConfiguration Database { get; set; } = new();
    public CommunicationConfiguration Communication { get; set; } = new();
    public SecurityConfiguration Security { get; set; } = new();
    public List<ModuleConfiguration> Modules { get; set; } = new();
    public LoggingConfiguration Logging { get; set; } = new();
}

public class DatabaseConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public TimeSpan DataRetentionPeriod { get; set; } = TimeSpan.FromDays(730); // 2 Jahre
    public int CompressionAfterDays { get; set; } = 7;
    public int MaxConnections { get; set; } = 100;
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

public class CommunicationConfiguration
{
    public MqttConfiguration Mqtt { get; set; } = new();
    public GrpcConfiguration Grpc { get; set; } = new();
}

public class MqttConfiguration
{
    public string BrokerHost { get; set; } = "localhost";
    public int BrokerPort { get; set; } = 1883;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseTls { get; set; } = true;
    public string ClientId { get; set; } = Environment.MachineName;
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(60);
}
```

##### 6. Sicherheitsarchitektur

###### Authentifizierung und Autorisierung
```csharp
public class EMSSecurityConfiguration
{
    public JwtConfiguration Jwt { get; set; } = new();
    public TlsConfiguration Tls { get; set; } = new();
    public DeviceCertificateConfiguration DeviceCertificates { get; set; } = new();
}

public class JwtConfiguration
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "EMSCore";
    public string Audience { get; set; } = "EMSCore";
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(8);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
}

// Rollenbasierte Autorisierung
public static class EMSRoles
{
    public const string SystemAdmin = "SystemAdmin";
    public const string SiteManager = "SiteManager";
    public const string Operator = "Operator";
    public const string ReadOnly = "ReadOnly";
    public const string Device = "Device";
}

public static class EMSPolicies
{
    public const string ManageBatteries = "ManageBatteries";
    public const string ViewEnergyData = "ViewEnergyData";
    public const string ConfigureDevices = "ConfigureDevices";
    public const string ManageUsers = "ManageUsers";
}
```

###### Verschlüsselung
- **Transport:** TLS 1.3 für alle Kommunikation
- **At Rest:** AES-256 für sensitive Konfigurationsdaten
- **Device Authentication:** X.509 Zertifikate für Edge-Systeme
- **API Security:** JWT Bearer Tokens mit Refresh-Mechanismus

##### 7. Monitoring und Observability

###### Metriken
```csharp
public class EMSMetrics
{
    private readonly Counter _energyDataPointsReceived;
    private readonly Histogram _commandExecutionTime;
    private readonly Gauge _activeBatteryModules;
    private readonly Gauge _systemHealth;
    
    public void RecordEnergyDataPoint(string deviceId, string measurementType)
        => _energyDataPointsReceived.WithTags("device", deviceId, "type", measurementType).Increment();
    
    public void RecordCommandExecution(string command, double durationMs)
        => _commandExecutionTime.WithTags("command", command).Record(durationMs);
}
```

###### Health Checks
```csharp
public class EMSHealthChecks
{
    public static void ConfigureHealthChecks(IServiceCollection services, EMSConfiguration config)
    {
        services.AddHealthChecks()
            .AddNpgSql(config.Database.ConnectionString, name: "timescaledb")
            .AddCheck<MqttHealthCheck>("mqtt-broker")
            .AddCheck<GrpcHealthCheck>("grpc-services")
            .AddCheck<ModuleHealthCheck>("loaded-modules");
    }
}
```

##### 8. Deployment und Skalierung

###### Docker Configuration
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80 443 5000 5001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["EMSCore.csproj", "."]
RUN dotnet restore "EMSCore.csproj"
COPY . .
RUN dotnet build "EMSCore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EMSCore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EMSCore.dll"]
```

###### Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ems-backend
spec:
  replicas: 3
  selector:
    matchLabels:
      app: ems-backend
  template:
    metadata:
      labels:
        app: ems-backend
    spec:
      containers:
      - name: ems-backend
        image: emscore:latest
        ports:
        - containerPort: 80
        - containerPort: 5000
        env:
        - name: Database__ConnectionString
          valueFrom:
            secretKeyRef:
              name: ems-secrets
              key: database-connection
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
```

#### Performance-Anforderungen

##### Latenz
- **MQTT Nachrichten:** < 100ms Ende-zu-Ende
- **gRPC Kommandos:** < 500ms Response Time
- **Datenbankabfragen:** < 1s für komplexe Aggregationen
- **Plugin-Initialisierung:** < 30s pro Modul

##### Durchsatz
- **Sensor-Daten:** 10,000 Messpunkte/Sekunde pro Edge-System
- **Gleichzeitige Verbindungen:** 1,000 MQTT-Clients pro Broker
- **API-Requests:** 1,000 Requests/Sekunde
- **Datenbank-Writes:** 50,000 Inserts/Sekunde

##### Skalierbarkeit
- **Horizontale Skalierung:** Backend-Services über Load Balancer
- **Datenbank-Skalierung:** TimescaleDB Clustering
- **Edge-Systeme:** Bis zu 1,000 Edge-Nodes pro Backend
- **Plugin-Isolation:** Separate AppDomains/Prozesse für kritische Module

#### Erweiterbarkeit und Zukunftssicherheit

##### Plugin-Ökosystem
- **Hot-Swapping:** Module zur Laufzeit laden/entladen
- **Versionierung:** Semantic Versioning mit Kompatibilitätsprüfung
- **Marketplace:** Plugin-Repository mit digitalen Signaturen
- **SDK:** Entwickler-Tools und Templates

##### API-Evolution
- **Versionierung:** API-Versioning über Header und URL-Pfade
- **Backward Compatibility:** Mindestens 2 Major Versions
- **GraphQL:** Optionale GraphQL-Schicht für flexible Abfragen
- **Webhooks:** Event-basierte Integration mit externen Systemen

##### Integration
- **REST APIs:** OpenAPI 3.0 Spezifikation
- **Message Queues:** RabbitMQ/Apache Kafka Integration
- **Cloud Services:** AWS IoT Core, Azure IoT Hub Adapter
- **Standards:** IEC 61850, Modbus, DNP3 Protokoll-Support