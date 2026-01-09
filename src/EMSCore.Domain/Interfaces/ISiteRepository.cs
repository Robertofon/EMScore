using EMSCore.Domain.Entities;

namespace EMSCore.Domain.Interfaces;

/// <summary>
/// Repository interface for site operations
/// </summary>
public interface ISiteRepository
{
    /// <summary>
    /// Gets all sites
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of sites</returns>
    Task<IEnumerable<Site>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a site by its identifier
    /// </summary>
    /// <param name="id">Site identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Site or null if not found</returns>
    Task<Site?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a site with its devices included
    /// </summary>
    /// <param name="id">Site identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Site with devices or null if not found</returns>
    Task<Site?> GetByIdWithDevicesAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only active sites
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active sites</returns>
    Task<IEnumerable<Site>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches sites by name or location
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching sites</returns>
    Task<IEnumerable<Site>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sites within a geographic radius
    /// </summary>
    /// <param name="latitude">Center latitude</param>
    /// <param name="longitude">Center longitude</param>
    /// <param name="radiusKm">Radius in kilometers</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of sites within radius</returns>
    Task<IEnumerable<Site>> GetSitesWithinRadiusAsync(
        double latitude, 
        double longitude, 
        double radiusKm, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new site
    /// </summary>
    /// <param name="site">Site to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Added site</returns>
    Task<Site> AddAsync(Site site, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing site
    /// </summary>
    /// <param name="site">Site to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated site</returns>
    Task<Site> UpdateAsync(Site site, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a site
    /// </summary>
    /// <param name="id">Site identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a site exists
    /// </summary>
    /// <param name="id">Site identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if site exists</returns>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets site statistics including device count and latest measurements
    /// </summary>
    /// <param name="id">Site identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Site statistics</returns>
    Task<SiteStatistics?> GetSiteStatisticsAsync(string id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics for a site
/// </summary>
public record SiteStatistics(
    string SiteId,
    string SiteName,
    int TotalDevices,
    int ActiveDevices,
    int OnlineDevices,
    DateTime? LastMeasurementAt,
    double? TotalEnergyProduction,
    double? TotalEnergyConsumption
);