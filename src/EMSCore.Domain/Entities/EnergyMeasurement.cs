using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EMSCore.Domain.Enums;

namespace EMSCore.Domain.Entities;

/// <summary>
/// Represents a time-series energy measurement from a device
/// This entity is designed for TimescaleDB hypertable storage
/// </summary>
[Table("energy_measurements")]
public class EnergyMeasurement
{
    /// <summary>
    /// Unique identifier for the measurement
    /// </summary>
    [Key]
    public long Id { get; set; }
    
    /// <summary>
    /// Timestamp when the measurement was taken (UTC)
    /// This is the primary time dimension for TimescaleDB
    /// </summary>
    [Column("timestamp")]
    [Required]
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Device that recorded this measurement
    /// </summary>
    [Column("device_id")]
    [MaxLength(100)]
    [Required]
    public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Site where the measurement was taken
    /// </summary>
    [Column("site_id")]
    [MaxLength(100)]
    [Required]
    public string SiteId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of measurement (voltage, current, power, etc.)
    /// </summary>
    [Column("measurement_type")]
    [Required]
    public MeasurementType Type { get; set; }
    
    /// <summary>
    /// Measured value
    /// </summary>
    [Column("value")]
    [Required]
    public double Value { get; set; }
    
    /// <summary>
    /// Unit of measurement (V, A, W, Hz, °C, %, etc.)
    /// </summary>
    [Column("unit")]
    [MaxLength(20)]
    [Required]
    public string Unit { get; set; } = string.Empty;
    
    /// <summary>
    /// Quality indicator for the measurement
    /// </summary>
    [Column("quality_flag")]
    public QualityFlag Quality { get; set; } = QualityFlag.Good;
    
    /// <summary>
    /// Additional metadata as JSON (optional)
    /// Can contain calibration info, sensor details, etc.
    /// </summary>
    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Phase information for multi-phase systems (L1, L2, L3, N)
    /// </summary>
    [Column("phase")]
    [MaxLength(10)]
    public string? Phase { get; set; }
    
    /// <summary>
    /// Aggregation level (raw, minute, hour, day)
    /// </summary>
    [Column("aggregation_level")]
    [MaxLength(20)]
    public string AggregationLevel { get; set; } = "raw";
    
    /// <summary>
    /// Number of samples used for aggregated values
    /// </summary>
    [Column("sample_count")]
    public int? SampleCount { get; set; }
    
    /// <summary>
    /// Minimum value in aggregation period (for aggregated data)
    /// </summary>
    [Column("min_value")]
    public double? MinValue { get; set; }
    
    /// <summary>
    /// Maximum value in aggregation period (for aggregated data)
    /// </summary>
    [Column("max_value")]
    public double? MaxValue { get; set; }
    
    /// <summary>
    /// When this record was inserted into the database
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    
    /// <summary>
    /// The device that recorded this measurement
    /// </summary>
    public virtual Device Device { get; set; } = null!;
    
    /// <summary>
    /// The site where this measurement was taken
    /// </summary>
    public virtual Site Site { get; set; } = null!;
}