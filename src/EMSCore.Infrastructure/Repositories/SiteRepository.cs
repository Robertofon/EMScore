using Microsoft.EntityFrameworkCore;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Interfaces;
using EMSCore.Infrastructure.Data;

namespace EMSCore.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for site operations using EF Core
/// Provides CRUD operations and specialized queries for site management including geographic operations
/// </summary>
public class SiteRepository : ISiteRepository
{
    private readonly EMSDbContext _context;

    /// <summary>
    /// Initializes a new instance of the SiteRepository
    /// </summary>
    /// <param name="context">EF Core database context</param>
    public SiteRepository(EMSDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all sites in the system
    /// Uses AsNoTracking for read-only operations to improve performance
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of all sites</returns>
    public async Task<IEnumerable<Site>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sites
            .AsNoTracking() // Read-only, no change tracking needed
            .OrderBy(s => s.Name) // Order by name for consistent results
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a specific site by its unique identifier
    /// Returns basic site information without related entities
    /// </summary>
    /// <param name="id">Unique site identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Site information, or null if not found</returns>
    public async Task<Site?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Sites
            .AsNoTracking() // Read-only operation
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves a site with all its associated devices
    /// Useful for comprehensive site management and device overview
    /// </summary>
    /// <param name="id">Unique site identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Site with all devices, or null if not found</returns>
    public async Task<Site?> GetByIdWithDevicesAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Sites
            .Include(s => s.Devices) // Eager load all devices at the site
            .AsNoTracking() // Read-only operation
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves only active sites (IsActive = true)
    /// Excludes decommissioned or disabled sites from operations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of active sites</returns>
    public async Task<IEnumerable<Site>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sites
            .Where(s => s.IsActive) // Only active sites
            .AsNoTracking() // Read-only operation
            .OrderBy(s => s.Name) // Consistent ordering
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Searches sites by name or location using case-insensitive partial matching
    /// Enables flexible site discovery and filtering
    /// </summary>
    /// <param name="searchTerm">Term to search for in name or location fields</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of sites matching the search criteria</returns>
    public async Task<IEnumerable<Site>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        
        return await _context.Sites
            .Where(s => s.Name.ToLower().Contains(lowerSearchTerm) || // Search in name
                       s.Location.ToLower().Contains(lowerSearchTerm)) // Search in location
            .AsNoTracking() // Read-only operation
            .OrderBy(s => s.Name) // Consistent ordering
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves sites within a specified geographic radius using the Haversine formula
    /// Critical for location-based services and regional management
    /// Only includes sites with valid GPS coordinates
    /// </summary>
    /// <param name="latitude">Center point latitude in decimal degrees</param>
    /// <param name="longitude">Center point longitude in decimal degrees</param>
    /// <param name="radiusKm">Search radius in kilometers</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of sites within the specified radius</returns>
    public async Task<IEnumerable<Site>> GetSitesWithinRadiusAsync(
        double latitude, 
        double longitude, 
        double radiusKm, 
        CancellationToken cancellationToken = default)
    {
        // Get all sites with valid coordinates first
        var sitesWithCoordinates = await _context.Sites
            .Where(s => s.Latitude.HasValue && s.Longitude.HasValue) // Only sites with GPS data
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Calculate distance using Haversine formula in memory
        // Note: For production, consider using PostGIS for database-level geographic calculations
        var sitesInRadius = sitesWithCoordinates
            .Where(s => CalculateDistance(latitude, longitude, s.Latitude!.Value, s.Longitude!.Value) <= radiusKm)
            .OrderBy(s => CalculateDistance(latitude, longitude, s.Latitude!.Value, s.Longitude!.Value)) // Order by distance
            .ToList();

        return sitesInRadius;
    }

    /// <summary>
    /// Adds a new site to the system
    /// Automatically sets creation and update timestamps
    /// </summary>
    /// <param name="site">Site entity to add</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Added site with generated timestamps</returns>
    public async Task<Site> AddAsync(Site site, CancellationToken cancellationToken = default)
    {
        // Set audit timestamps
        site.CreatedAt = DateTime.UtcNow;
        site.UpdatedAt = DateTime.UtcNow;
        
        // Add site to context
        _context.Sites.Add(site);
        await _context.SaveChangesAsync(cancellationToken);
        
        return site;
    }

    /// <summary>
    /// Updates an existing site in the system
    /// Automatically updates the modification timestamp
    /// </summary>
    /// <param name="site">Site entity with updated information</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated site</returns>
    public async Task<Site> UpdateAsync(Site site, CancellationToken cancellationToken = default)
    {
        // Update modification timestamp
        site.UpdatedAt = DateTime.UtcNow;
        
        // Update site in context
        _context.Sites.Update(site);
        await _context.SaveChangesAsync(cancellationToken);
        
        return site;
    }

    /// <summary>
    /// Removes a site from the system
    /// This will cascade delete related devices and measurements due to FK constraints
    /// Use with caution as this operation removes all historical data for the site
    /// </summary>
    /// <param name="id">Site identifier to delete</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if site was found and deleted, false otherwise</returns>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        // Find the site to delete
        var site = await _context.Sites
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            
        if (site == null)
            return false; // Site not found
            
        // Remove site (cascade delete will handle related devices and measurements)
        _context.Sites.Remove(site);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Checks if a site exists in the system
    /// Efficient existence check without loading the entire entity
    /// </summary>
    /// <param name="id">Site identifier to check</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if site exists, false otherwise</returns>
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Sites
            .AnyAsync(s => s.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves comprehensive statistics for a site including device counts and energy data
    /// Provides operational overview and health metrics for site management
    /// </summary>
    /// <param name="id">Site identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Site statistics or null if site not found</returns>
    public async Task<SiteStatistics?> GetSiteStatisticsAsync(string id, CancellationToken cancellationToken = default)
    {
        // Check if site exists
        var site = await _context.Sites
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            
        if (site == null)
            return null;

        // Get device statistics for the site
        var deviceStats = await _context.Devices
            .Where(d => d.SiteId == id)
            .GroupBy(d => 1) // Group all devices together
            .Select(g => new
            {
                TotalDevices = g.Count(),
                ActiveDevices = g.Count(d => d.IsActive),
                OnlineDevices = g.Count(d => d.IsOnline && d.IsActive)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Get latest measurement timestamp for the site
        var lastMeasurement = await _context.EnergyMeasurements
            .Where(m => m.SiteId == id)
            .OrderByDescending(m => m.Timestamp)
            .Select(m => m.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        // Get energy production and consumption totals for today
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        
        var energyStats = await _context.EnergyMeasurements
            .Where(m => m.SiteId == id && 
                       m.Timestamp >= today && 
                       m.Timestamp < tomorrow &&
                       (m.Type == Domain.Enums.MeasurementType.EnergyProduction || 
                        m.Type == Domain.Enums.MeasurementType.EnergyConsumption))
            .GroupBy(m => m.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(m => m.Value) })
            .ToListAsync(cancellationToken);

        var totalProduction = energyStats
            .Where(e => e.Type == Domain.Enums.MeasurementType.EnergyProduction)
            .Sum(e => e.Total);
            
        var totalConsumption = energyStats
            .Where(e => e.Type == Domain.Enums.MeasurementType.EnergyConsumption)
            .Sum(e => e.Total);

        // Build and return statistics
        return new SiteStatistics(
            SiteId: site.Id,
            SiteName: site.Name,
            TotalDevices: deviceStats?.TotalDevices ?? 0,
            ActiveDevices: deviceStats?.ActiveDevices ?? 0,
            OnlineDevices: deviceStats?.OnlineDevices ?? 0,
            LastMeasurementAt: lastMeasurement == default ? null : lastMeasurement,
            TotalEnergyProduction: totalProduction > 0 ? totalProduction : null,
            TotalEnergyConsumption: totalConsumption > 0 ? totalConsumption : null
        );
    }

    /// <summary>
    /// Calculates the distance between two geographic points using the Haversine formula
    /// Returns distance in kilometers with good accuracy for most use cases
    /// </summary>
    /// <param name="lat1">Latitude of first point in decimal degrees</param>
    /// <param name="lon1">Longitude of first point in decimal degrees</param>
    /// <param name="lat2">Latitude of second point in decimal degrees</param>
    /// <param name="lon2">Longitude of second point in decimal degrees</param>
    /// <returns>Distance in kilometers</returns>
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0; // Earth's radius in kilometers
        
        // Convert degrees to radians
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        
        lat1 = DegreesToRadians(lat1);
        lat2 = DegreesToRadians(lat2);
        
        // Haversine formula
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return earthRadiusKm * c;
    }

    /// <summary>
    /// Converts degrees to radians for geographic calculations
    /// </summary>
    /// <param name="degrees">Angle in degrees</param>
    /// <returns>Angle in radians</returns>
    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}