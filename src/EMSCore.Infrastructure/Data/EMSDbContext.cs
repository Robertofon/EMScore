using Microsoft.EntityFrameworkCore;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Enums;

namespace EMSCore.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for EMS Core with TimescaleDB support
/// </summary>
public class EMSDbContext : DbContext
{
    public EMSDbContext(DbContextOptions<EMSDbContext> options) : base(options)
    {
    }

    // DbSets for entities
    public DbSet<Site> Sites { get; set; } = null!;
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<EnergyMeasurement> EnergyMeasurements { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Site entity
        ConfigureSite(modelBuilder);
        
        // Configure Device entity
        ConfigureDevice(modelBuilder);
        
        // Configure EnergyMeasurement entity (TimescaleDB hypertable)
        ConfigureEnergyMeasurement(modelBuilder);
    }

    private static void ConfigureSite(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Site>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasMaxLength(100)
                .IsRequired();
                
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();
                
            entity.Property(e => e.Location)
                .HasMaxLength(500);
                
            entity.Property(e => e.TimeZone)
                .HasMaxLength(50)
                .HasDefaultValue("UTC");
                
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
                
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.Latitude, e.Longitude })
                .HasDatabaseName("IX_Sites_Location");
        });
    }

    private static void ConfigureDevice(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasMaxLength(100)
                .IsRequired();
                
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();
                
            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .IsRequired();
                
            entity.Property(e => e.Manufacturer)
                .HasMaxLength(100);
                
            entity.Property(e => e.Model)
                .HasMaxLength(100);
                
            entity.Property(e => e.SerialNumber)
                .HasMaxLength(100);
                
            entity.Property(e => e.FirmwareVersion)
                .HasMaxLength(50);
                
            entity.Property(e => e.SiteId)
                .HasMaxLength(100)
                .IsRequired();
                
            entity.Property(e => e.Configuration)
                .HasColumnType("jsonb");
                
            entity.Property(e => e.Capabilities)
                .HasColumnType("jsonb");
                
            entity.Property(e => e.Location)
                .HasMaxLength(200);
                
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
                
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
                
            entity.Property(e => e.IsOnline)
                .HasDefaultValue(false);
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relationships
            entity.HasOne(d => d.Site)
                .WithMany(s => s.Devices)
                .HasForeignKey(d => d.SiteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.SiteId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsOnline);
            entity.HasIndex(e => e.LastSeenAt);
            entity.HasIndex(e => new { e.SiteId, e.Type })
                .HasDatabaseName("IX_Devices_Site_Type");
        });
    }

    private static void ConfigureEnergyMeasurement(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EnergyMeasurement>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Timestamp)
                .IsRequired()
                .HasColumnName("timestamp");
                
            entity.Property(e => e.DeviceId)
                .HasMaxLength(100)
                .IsRequired()
                .HasColumnName("device_id");
                
            entity.Property(e => e.SiteId)
                .HasMaxLength(100)
                .IsRequired()
                .HasColumnName("site_id");
                
            entity.Property(e => e.Type)
                .IsRequired()
                .HasColumnName("measurement_type")
                .HasConversion<int>();
                
            entity.Property(e => e.Value)
                .IsRequired()
                .HasColumnName("value");
                
            entity.Property(e => e.Unit)
                .HasMaxLength(20)
                .IsRequired()
                .HasColumnName("unit");
                
            entity.Property(e => e.Quality)
                .HasColumnName("quality_flag")
                .HasConversion<int>()
                .HasDefaultValue(QualityFlag.Good);
                
            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb");
                
            entity.Property(e => e.Phase)
                .HasMaxLength(10)
                .HasColumnName("phase");
                
            entity.Property(e => e.AggregationLevel)
                .HasMaxLength(20)
                .HasColumnName("aggregation_level")
                .HasDefaultValue("raw");
                
            entity.Property(e => e.SampleCount)
                .HasColumnName("sample_count");
                
            entity.Property(e => e.MinValue)
                .HasColumnName("min_value");
                
            entity.Property(e => e.MaxValue)
                .HasColumnName("max_value");
                
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relationships
            entity.HasOne(e => e.Device)
                .WithMany(d => d.Measurements)
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Site)
                .WithMany(s => s.Measurements)
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Cascade);

            // TimescaleDB optimized indexes
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_EnergyMeasurements_Timestamp");
                
            entity.HasIndex(e => new { e.DeviceId, e.Timestamp })
                .HasDatabaseName("IX_EnergyMeasurements_Device_Time");
                
            entity.HasIndex(e => new { e.SiteId, e.Timestamp })
                .HasDatabaseName("IX_EnergyMeasurements_Site_Time");
                
            entity.HasIndex(e => new { e.DeviceId, e.Type, e.Timestamp })
                .HasDatabaseName("IX_EnergyMeasurements_Device_Type_Time");
                
            entity.HasIndex(e => new { e.SiteId, e.Type, e.Timestamp })
                .HasDatabaseName("IX_EnergyMeasurements_Site_Type_Time");
                
            entity.HasIndex(e => e.AggregationLevel)
                .HasDatabaseName("IX_EnergyMeasurements_AggregationLevel");

            // Table comment for TimescaleDB
            entity.ToTable(tb => tb.HasComment("TimescaleDB Hypertable for energy measurements"));
        });
    }

    /// <summary>
    /// Configures TimescaleDB hypertable for energy measurements
    /// This should be called after database creation
    /// </summary>
    public async Task ConfigureTimescaleDbAsync()
    {
        // Create TimescaleDB hypertable for energy_measurements
        var createHypertableSql = @"
            SELECT create_hypertable('energy_measurements', 'timestamp', 
                chunk_time_interval => INTERVAL '1 day',
                if_not_exists => TRUE);";

        await Database.ExecuteSqlRawAsync(createHypertableSql);

        // Create compression policy (compress data older than 7 days)
        var compressionPolicySql = @"
            SELECT add_compression_policy('energy_measurements', INTERVAL '7 days', if_not_exists => TRUE);";

        try
        {
            await Database.ExecuteSqlRawAsync(compressionPolicySql);
        }
        catch
        {
            // Compression might not be available in all TimescaleDB versions
            // This is optional, so we can ignore errors
        }

        // Create retention policy (delete data older than 2 years)
        var retentionPolicySql = @"
            SELECT add_retention_policy('energy_measurements', INTERVAL '2 years', if_not_exists => TRUE);";

        try
        {
            await Database.ExecuteSqlRawAsync(retentionPolicySql);
        }
        catch
        {
            // Retention policy might not be available in all TimescaleDB versions
            // This is optional, so we can ignore errors
        }
    }

    /// <summary>
    /// Creates materialized views for common aggregations
    /// </summary>
    public async Task CreateMaterializedViewsAsync()
    {
        // Hourly aggregations
        var hourlyViewSql = @"
            CREATE MATERIALIZED VIEW IF NOT EXISTS energy_measurements_hourly
            WITH (timescaledb.continuous) AS
            SELECT 
                time_bucket('1 hour', timestamp) AS bucket,
                device_id,
                site_id,
                measurement_type,
                unit,
                AVG(value) as avg_value,
                MIN(value) as min_value,
                MAX(value) as max_value,
                COUNT(*) as sample_count
            FROM energy_measurements
            WHERE aggregation_level = 'raw'
            GROUP BY bucket, device_id, site_id, measurement_type, unit
            WITH NO DATA;";

        await Database.ExecuteSqlRawAsync(hourlyViewSql);

        // Daily aggregations
        var dailyViewSql = @"
            CREATE MATERIALIZED VIEW IF NOT EXISTS energy_measurements_daily
            WITH (timescaledb.continuous) AS
            SELECT 
                time_bucket('1 day', timestamp) AS bucket,
                device_id,
                site_id,
                measurement_type,
                unit,
                AVG(value) as avg_value,
                MIN(value) as min_value,
                MAX(value) as max_value,
                COUNT(*) as sample_count
            FROM energy_measurements
            WHERE aggregation_level = 'raw'
            GROUP BY bucket, device_id, site_id, measurement_type, unit
            WITH NO DATA;";

        await Database.ExecuteSqlRawAsync(dailyViewSql);
    }
}