using Microsoft.AspNetCore.Mvc;
using MediatR;
using EMSCore.Application.Queries;
using EMSCore.Application.Commands;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Enums;

namespace EMSCore.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BackendController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BackendController> _logger;

    public BackendController(IMediator mediator, ILogger<BackendController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            service = "EMSCore.Backend",
            timestamp = DateTime.UtcNow
        });
    }

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

    [HttpGet("edges")]
    public IActionResult GetConnectedEdges()
    {
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

    [HttpGet("sites")]
    public async Task<ActionResult<IEnumerable<Site>>> GetAllSites(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting all sites");
            var sites = await _mediator.Send(new GetAllSitesQuery(), cancellationToken);
            return Ok(sites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all sites");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("sites/{siteId}")]
    public async Task<ActionResult<Site>> GetSite(string siteId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting site {SiteId}", siteId);
            var site = await _mediator.Send(new GetSiteByIdQuery(siteId), cancellationToken);
            
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

    [HttpPost("sites")]
    public async Task<ActionResult<Site>> CreateSite([FromBody] CreateSiteCommand command, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating site {SiteId}", command.Id);
            var site = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetSite), new { siteId = site.Id }, site);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating site {SiteId}", command.Id);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating site {SiteId}", command.Id);
            return StatusCode(500, "Internal server error");
        }
    }

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

            var range = new DateTimeRange(startTime, endTime);
            var query = new GetSiteEnergyMeasurementsQuery(siteId, range);
            
            var measurements = await _mediator.Send(query, cancellationToken);
            
            return Ok(measurements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving aggregated data for site {SiteId}", siteId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("statistics/site/{siteId}")]
    public async Task<ActionResult<SiteStatistics>> GetSiteStatistics(
        string siteId,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting statistics for site {SiteId}", siteId);

            if (startTime >= endTime)
            {
                return BadRequest("Start time must be before end time");
            }

            var range = new DateTimeRange(startTime, endTime);
            var query = new GetSiteEnergyMeasurementsQuery(siteId, range);
            
            var measurements = await _mediator.Send(query, cancellationToken);
            
            var stats = new SiteStatistics
            {
                SiteId = siteId,
                TotalDevices = measurements.Select(m => m.DeviceId).Distinct().Count(),
                TotalMeasurements = measurements.Count(),
                TimeRange = new { Start = startTime, End = endTime },
                GeneratedAt = DateTime.UtcNow
            };
            
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for site {SiteId}", siteId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("dashboard/summary")]
    public async Task<ActionResult<DashboardSummary>> GetDashboardSummary(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting dashboard summary");

            var summary = new DashboardSummary
            {
                TotalSites = 2,
                TotalDevices = 8,
                ActiveEdges = 2,
                MessagesPerMinute = 150,
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

public class GetAllSitesQuery : IRequest<IEnumerable<Site>> { }

public class GetSiteByIdQuery : IRequest<Site?>
{
    public string SiteId { get; }

    public GetSiteByIdQuery(string siteId)
    {
        SiteId = siteId;
    }
}

public class SiteStatistics
{
    public string SiteId { get; set; } = string.Empty;
    public int TotalDevices { get; set; }
    public int TotalMeasurements { get; set; }
    public object? TimeRange { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class DashboardSummary
{
    public int TotalSites { get; set; }
    public int TotalDevices { get; set; }
    public int ActiveEdges { get; set; }
    public int MessagesPerMinute { get; set; }
    public DateTime Timestamp { get; set; }
}
