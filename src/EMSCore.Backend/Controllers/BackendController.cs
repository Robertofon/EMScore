using Microsoft.AspNetCore.Mvc;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Interfaces;

namespace EMSCore.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BackendController : ControllerBase
{
    private readonly ISiteRepository _siteRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ILogger<BackendController> _logger;

    public BackendController(
        ISiteRepository siteRepository,
        IDeviceRepository deviceRepository,
        ILogger<BackendController> logger)
    {
        _siteRepository = siteRepository;
        _deviceRepository = deviceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets system status and capabilities
    /// </summary>
    [HttpGet("system/status")]
    public IActionResult GetSystemStatus()
    {
        return Ok(new
        {
            service = "EMSCore.Backend",
            version = "1.0.0",
            description = "Zentrale Datenverarbeitung und Systemverwaltung",
            timestamp = DateTime.UtcNow,
            features = new[]
            {
                "MQTT Data Ingestion from Edge Systems",
                "Centralized Data Storage",
                "Multi-Site Aggregation",
                "TimescaleDB Analytics",
                "Health Monitoring"
            }
        });
    }

    /// <summary>
    /// Gets list of connected Edge systems (placeholder - requires MQTT discovery)
    /// </summary>
    [HttpGet("edges")]
    public IActionResult GetConnectedEdges()
    {
        // TODO: Implement MQTT-based edge discovery
        // Currently returns placeholder data until MQTT LWT messages are integrated
        return Ok(new
        {
            edges = new[]
            {
                new { id = "edge-001", name = "Site 1 Edge", status = "online", lastSeen = DateTime.UtcNow },
                new { id = "edge-002", name = "Site 2 Edge", status = "online", lastSeen = DateTime.UtcNow }
            },
            totalCount = 2,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Gets all sites in the system
    /// </summary>
    [HttpGet("sites")]
    public async Task<ActionResult<IEnumerable<Site>>> GetAllSites(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting all sites");
            var sites = await _siteRepository.GetAllAsync(cancellationToken);
            return Ok(sites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all sites");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific site by ID
    /// </summary>
    /// <param name="siteId">Site identifier</param>
    [HttpGet("sites/{siteId}")]
    public async Task<ActionResult<Site>> GetSite(string siteId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting site {SiteId}", siteId);
            var site = await _siteRepository.GetByIdAsync(siteId, cancellationToken);
            
            if (site == null)
            {
                return NotFound($"Site with ID '{siteId}' not found");
            }
            
            return Ok(site);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving site {SiteId}", siteId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a new site
    /// </summary>
    /// <param name="site">Site data</param>
    [HttpPost("sites")]
    public async Task<ActionResult<Site>> CreateSite([FromBody] Site site, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating site {SiteId}", site.Id);
            
            if (await _siteRepository.ExistsAsync(site.Id, cancellationToken))
            {
                return Conflict($"Site with ID '{site.Id}' already exists");
            }
            
            var createdSite = await _siteRepository.AddAsync(site, cancellationToken);
            return CreatedAtAction(nameof(GetSite), new { siteId = createdSite.Id }, createdSite);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating site {SiteId}", site.Id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets aggregated energy data for a specific site
    /// </summary>
    /// <param name="siteId">Site identifier</param>
    /// <param name="startTime">Start of time range</param>
    /// <param name="endTime">End of time range</param>
    /// <param name="intervalMinutes">Aggregation interval in minutes</param>
    [HttpGet("aggregate/site/{siteId}")]
    public async Task<ActionResult<IEnumerable<EnergyMeasurement>>> GetAggregatedSiteData(
        string siteId,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        [FromQuery] int intervalMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting aggregated data for site {SiteId}", siteId);

            if (startTime >= endTime)
            {
                return BadRequest("Start time must be before end time");
            }

            // TODO: Implement aggregation using TimescaleDB time_bucket
            // Currently returns raw measurements
            var site = await _siteRepository.GetByIdAsync(siteId, cancellationToken);
            if (site == null)
            {
                return NotFound($"Site '{siteId}' not found");
            }

            var range = new DateTimeRange(startTime, endTime);
            var devices = await _deviceRepository.GetBySiteIdAsync(siteId, cancellationToken);
            var measurements = new List<EnergyMeasurement>();

            foreach (var device in devices)
            {
                // Placeholder - would use IEnergyMeasurementRepository in production
                _logger.LogDebug("Would fetch measurements for device {DeviceId}", device.Id);
            }
            
            return Ok(measurements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving aggregated data for site {SiteId}", siteId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets detailed statistics for a specific site
    /// </summary>
    /// <param name="siteId">Site identifier</param>
    [HttpGet("statistics/site/{siteId}")]
    public async Task<ActionResult<Domain.Interfaces.SiteStatistics>> GetSiteStatistics(
        string siteId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting statistics for site {SiteId}", siteId);

            var stats = await _siteRepository.GetSiteStatisticsAsync(siteId, cancellationToken);
            
            if (stats == null)
            {
                return NotFound($"Site '{siteId}' not found");
            }
            
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for site {SiteId}", siteId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets dashboard summary with system-wide metrics (placeholder)
    /// </summary>
    /// <remarks>
    /// TODO: Implement actual metrics collection from database
    /// Currently returns placeholder values
    /// </remarks>
    [HttpGet("dashboard/summary")]
    public async Task<ActionResult<DashboardSummary>> GetDashboardSummary(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting dashboard summary");

            var sites = await _siteRepository.GetAllAsync(cancellationToken);
            var devices = await _deviceRepository.GetAllAsync(cancellationToken);
            var activeDevices = await _deviceRepository.GetActiveAsync(cancellationToken);
            
            var summary = new DashboardSummary
            {
                TotalSites = sites.Count(),
                TotalDevices = devices.Count(),
                ActiveDevices = activeDevices.Count(),
                MessagesPerMinute = 0, // TODO: Implement MQTT message counter
                Timestamp = DateTime.UtcNow
            };
            
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard summary");
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Dashboard summary with system-wide metrics
/// </summary>
public class DashboardSummary
{
    public int TotalSites { get; set; }
    public int TotalDevices { get; set; }
    public int ActiveDevices { get; set; }
    public int MessagesPerMinute { get; set; }
    public DateTime Timestamp { get; set; }
}
