using Microsoft.AspNetCore.Mvc;
using MediatR;
using EMSCore.Application.Queries;
using EMSCore.Application.Commands;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Enums;
using DateTimeRange;

namespace EMSCore.Edge.Controllers;

/// <summary>
/// REST API controller for energy measurement operations
/// Provides endpoints for retrieving and managing energy data
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EnergyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EnergyController> _logger;

    /// <summary>
    /// Initializes a new instance of the EnergyController
    /// </summary>
    /// <param name="mediator">MediatR mediator for CQRS operations</param>
    /// <param name="logger">Logger for controller operations</param>
    public EnergyController(IMediator mediator, ILogger<EnergyController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets energy measurements for a specific device within a time range
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start of time range (ISO 8601 format)</param>
    /// <param name="endTime">End of time range (ISO 8601 format)</param>
    /// <param name="measurementType">Optional filter by measurement type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of energy measurements</returns>
    /// <response code="200">Returns the energy measurements</response>
    /// <response code="400">Invalid parameters provided</response>
    /// <response code="404">Device not found</response>
    [HttpGet("devices/{deviceId}/measurements")]
    [ProducesResponseType(typeof(IEnumerable<EnergyMeasurement>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnergyMeasurement>>> GetDeviceMeasurements(
        string deviceId,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        [FromQuery] MeasurementType? measurementType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting measurements for device {DeviceId} from {StartTime} to {EndTime}", 
                deviceId, startTime, endTime);

            // Validate time range
            if (startTime >= endTime)
            {
                return BadRequest("Start time must be before end time");
            }

            var range = new DateTimeRange.DateTimeRange(startTime, endTime);
            var query = new GetEnergyMeasurementsQuery(deviceId, range, measurementType);
            
            var measurements = await _mediator.Send(query, cancellationToken);
            
            return Ok(measurements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving measurements for device {DeviceId}", deviceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets aggregated energy measurements for a device using TimescaleDB time_bucket
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="startTime">Start of time range (ISO 8601 format)</param>
    /// <param name="endTime">End of time range (ISO 8601 format)</param>
    /// <param name="intervalMinutes">Aggregation interval in minutes (e.g., 15, 60)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of aggregated energy measurements</returns>
    /// <response code="200">Returns the aggregated measurements</response>
    /// <response code="400">Invalid parameters provided</response>
    [HttpGet("devices/{deviceId}/measurements/aggregated")]
    [ProducesResponseType(typeof(IEnumerable<EnergyMeasurement>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<EnergyMeasurement>>> GetAggregatedMeasurements(
        string deviceId,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        [FromQuery] int intervalMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting aggregated measurements for device {DeviceId} with {Interval}min intervals", 
                deviceId, intervalMinutes);

            // Validate parameters
            if (startTime >= endTime)
            {
                return BadRequest("Start time must be before end time");
            }

            if (intervalMinutes <= 0 || intervalMinutes > 1440) // Max 24 hours
            {
                return BadRequest("Interval must be between 1 and 1440 minutes");
            }

            var range = new DateTimeRange.DateTimeRange(startTime, endTime);
            var interval = TimeSpan.FromMinutes(intervalMinutes);
            var query = new GetAggregatedEnergyDataQuery(deviceId, range, interval);
            
            var measurements = await _mediator.Send(query, cancellationToken);
            
            return Ok(measurements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving aggregated measurements for device {DeviceId}", deviceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets energy measurements for an entire site
    /// </summary>
    /// <param name="siteId">Site identifier</param>
    /// <param name="startTime">Start of time range (ISO 8601 format)</param>
    /// <param name="endTime">End of time range (ISO 8601 format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of energy measurements for the site</returns>
    /// <response code="200">Returns the site measurements</response>
    /// <response code="400">Invalid parameters provided</response>
    [HttpGet("sites/{siteId}/measurements")]
    [ProducesResponseType(typeof(IEnumerable<EnergyMeasurement>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<EnergyMeasurement>>> GetSiteMeasurements(
        string siteId,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting measurements for site {SiteId} from {StartTime} to {EndTime}", 
                siteId, startTime, endTime);

            // Validate time range
            if (startTime >= endTime)
            {
                return BadRequest("Start time must be before end time");
            }

            var range = new DateTimeRange.DateTimeRange(startTime, endTime);
            var query = new GetSiteEnergyMeasurementsQuery(siteId, range);
            
            var measurements = await _mediator.Send(query, cancellationToken);
            
            return Ok(measurements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving measurements for site {SiteId}", siteId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets the latest measurement for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="measurementType">Optional filter by measurement type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest energy measurement or null if none found</returns>
    /// <response code="200">Returns the latest measurement</response>
    /// <response code="404">No measurements found</response>
    [HttpGet("devices/{deviceId}/measurements/latest")]
    [ProducesResponseType(typeof(EnergyMeasurement), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnergyMeasurement?>> GetLatestMeasurement(
        string deviceId,
        [FromQuery] MeasurementType? measurementType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting latest measurement for device {DeviceId}", deviceId);

            var query = new GetLatestMeasurementQuery(deviceId, measurementType);
            var measurement = await _mediator.Send(query, cancellationToken);
            
            if (measurement == null)
            {
                return NotFound($"No measurements found for device {deviceId}");
            }
            
            return Ok(measurement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest measurement for device {DeviceId}", deviceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets measurement statistics for a device and time range
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="measurementType">Type of measurement to analyze</param>
    /// <param name="startTime">Start of time range (ISO 8601 format)</param>
    /// <param name="endTime">End of time range (ISO 8601 format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Measurement statistics including average, min, max, count</returns>
    /// <response code="200">Returns the measurement statistics</response>
    /// <response code="400">Invalid parameters provided</response>
    [HttpGet("devices/{deviceId}/measurements/statistics")]
    [ProducesResponseType(typeof(MeasurementStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MeasurementStatistics>> GetMeasurementStatistics(
        string deviceId,
        [FromQuery] MeasurementType measurementType,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting statistics for device {DeviceId}, type {MeasurementType}", 
                deviceId, measurementType);

            // Validate time range
            if (startTime >= endTime)
            {
                return BadRequest("Start time must be before end time");
            }

            var range = new DateTimeRange.DateTimeRange(startTime, endTime);
            var query = new GetMeasurementStatisticsQuery(deviceId, measurementType, range);
            
            var statistics = await _mediator.Send(query, cancellationToken);
            
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for device {DeviceId}", deviceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Stores a single energy measurement
    /// </summary>
    /// <param name="measurement">Energy measurement to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stored measurement with generated ID</returns>
    /// <response code="201">Measurement created successfully</response>
    /// <response code="400">Invalid measurement data</response>
    [HttpPost("measurements")]
    [ProducesResponseType(typeof(EnergyMeasurement), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EnergyMeasurement>> StoreMeasurement(
        [FromBody] EnergyMeasurement measurement,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Storing measurement for device {DeviceId}: {Type} = {Value} {Unit}", 
                measurement.DeviceId, measurement.Type, measurement.Value, measurement.Unit);

            // Validate measurement
            if (string.IsNullOrEmpty(measurement.DeviceId) || string.IsNullOrEmpty(measurement.SiteId))
            {
                return BadRequest("DeviceId and SiteId are required");
            }

            var command = new StoreMeasurementCommand(measurement);
            var storedMeasurement = await _mediator.Send(command, cancellationToken);
            
            return CreatedAtAction(
                nameof(GetLatestMeasurement), 
                new { deviceId = storedMeasurement.DeviceId }, 
                storedMeasurement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing measurement for device {DeviceId}", measurement.DeviceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Stores multiple energy measurements in batch
    /// </summary>
    /// <param name="measurements">Collection of measurements to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of measurements stored</returns>
    /// <response code="201">Measurements created successfully</response>
    /// <response code="400">Invalid measurement data</response>
    [HttpPost("measurements/batch")]
    [ProducesResponseType(typeof(BatchStoreResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BatchStoreResult>> StoreMeasurementsBatch(
        [FromBody] IEnumerable<EnergyMeasurement> measurements,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var measurementList = measurements.ToList();
            _logger.LogDebug("Storing batch of {Count} measurements", measurementList.Count);

            // Validate measurements
            if (!measurementList.Any())
            {
                return BadRequest("At least one measurement is required");
            }

            var command = new StoreMeasurementsBatchCommand(measurementList);
            var storedCount = await _mediator.Send(command, cancellationToken);
            
            var result = new BatchStoreResult(storedCount, measurementList.Count);
            return CreatedAtAction(nameof(StoreMeasurementsBatch), result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing batch measurements");
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Result of batch measurement storage operation
/// </summary>
/// <param name="StoredCount">Number of measurements successfully stored</param>
/// <param name="TotalCount">Total number of measurements in the batch</param>
public record BatchStoreResult(int StoredCount, int TotalCount);