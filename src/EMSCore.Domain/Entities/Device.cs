using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMSCore.Domain.Entities;

/// <summary>
/// Represents a physical device that can measure or control energy
/// </summary>
[Table("devices")]
public class Device
{
    /// <summary>
    /// Unique identifier for the device
    /// </summary>
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable name of the device
    /// </summary>
    [MaxLength(200)]
    [Required]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of device (e.g., "Battery", "Solar Panel", "Inverter", "Sensor")
    /// </summary>
    [MaxLength(100)]
    [Required]
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Device manufacturer
    /// </summary>
    [MaxLength(100)]
    public string? Manufacturer { get; set; }
    
    /// <summary>
    /// Device model number
    /// </summary>
    [MaxLength(100)]
    public string? Model { get; set; }
    
    /// <summary>
    /// Device serial number
    /// </summary>
    [MaxLength(100)]
    public string? SerialNumber { get; set; }
    
    /// <summary>
    /// Firmware version
    /// </summary>
    [MaxLength(50)]
    public string? FirmwareVersion { get; set; }
    
    /// <summary>
    /// Site where this device is located
    /// </summary>
    [MaxLength(100)]
    [Required]
    public string SiteId { get; set; } = string.Empty;
    
    /// <summary>
    /// Device configuration as JSON
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Configuration { get; set; }
    
    /// <summary>
    /// Device capabilities as JSON array
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Capabilities { get; set; }
    
    /// <summary>
    /// Physical location within the site
    /// </summary>
    [MaxLength(200)]
    public string? Location { get; set; }
    
    /// <summary>
    /// Device description or notes
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Indicates if the device is currently active and operational
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Indicates if the device is currently online
    /// </summary>
    public bool IsOnline { get; set; } = false;
    
    /// <summary>
    /// Last time the device was seen online
    /// </summary>
    public DateTime? LastSeenAt { get; set; }
    
    /// <summary>
    /// When the device was created in the system
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the device was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    
    /// <summary>
    /// The site where this device is located
    /// </summary>
    public virtual Site Site { get; set; } = null!;
    
    /// <summary>
    /// Collection of measurements from this device
    /// </summary>
    public virtual ICollection<EnergyMeasurement> Measurements { get; set; } = new List<EnergyMeasurement>();
}