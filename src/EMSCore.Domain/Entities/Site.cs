using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMSCore.Domain.Entities;

/// <summary>
/// Represents a physical site or location where energy management occurs
/// </summary>
[Table("sites")]
public class Site
{
    /// <summary>
    /// Unique identifier for the site
    /// </summary>
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable name of the site
    /// </summary>
    [MaxLength(200)]
    [Required]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Physical location description
    /// </summary>
    [MaxLength(500)]
    public string Location { get; set; } = string.Empty;
    
    /// <summary>
    /// Latitude coordinate for GPS location
    /// </summary>
    public double? Latitude { get; set; }
    
    /// <summary>
    /// Longitude coordinate for GPS location
    /// </summary>
    public double? Longitude { get; set; }
    
    /// <summary>
    /// Timezone identifier (e.g., "Europe/Berlin")
    /// </summary>
    [MaxLength(50)]
    public string TimeZone { get; set; } = "UTC";
    
    /// <summary>
    /// Site description or notes
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Indicates if the site is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When the site was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the site was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    
    /// <summary>
    /// Collection of devices at this site
    /// </summary>
    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
    
    /// <summary>
    /// Collection of energy measurements from this site
    /// </summary>
    public virtual ICollection<EnergyMeasurement> Measurements { get; set; } = new List<EnergyMeasurement>();
}