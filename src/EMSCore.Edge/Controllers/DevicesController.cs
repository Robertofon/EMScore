using Microsoft.AspNetCore.Mvc;
using MediatR;
using EMSCore.Application.Commands;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Enums;
using EMSCore.Domain.Interfaces;

namespace EMSCore.Edge.Controllers;

/// <summary>
/// REST API controller for device management operations
/// Provides endpoints for creating, updating, and managing devices
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DevicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ILogger<DevicesController> _logger;

    /// <summary>
    /// Initializes a new instance of the DevicesController
    /// </summary>
    /// <param name="mediator">MediatR mediator for CQRS operations</param>
    /// <param name="deviceRepository">Repository for device queries</param>
    /// <param name="logger">Logger for controller operations</param>
    public DevicesController(
        IMediator mediator, 
        IDeviceRepository deviceRepository, 
        ILogger<DevicesController> logger)
    {
        _mediator = mediator;
        _deviceRepository = deviceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all devices in the system
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all devices</returns>
    /// <response code="200">Returns all devices</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Device>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Device>>> GetAllDevices(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting all devices");
            var devices = await _deviceRepository.GetAllAsync(cancellationToken);
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all devices");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific device by ID
    /// </summary>
    /// <param name="id">Device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Device information</returns>
    /// <response code="200">Returns the device</response>
    /// <response code="404">Device not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Device>> GetDevice(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting device {DeviceId}", id);
            var device = await _deviceRepository.GetByIdAsync(id, cancellationToken);
            
            if (device == null)
            {
                return NotFound($"Device with ID '{id}' not found");
            }
            
            return Ok(device);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device {DeviceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all devices at a specific site
    /// </summary>
    /// <param name="siteId">Site identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of devices at the site</returns>
    /// <response code="200">Returns devices at the site</response>
    [HttpGet("site/{siteId}")]
    [ProducesResponseType(typeof(IEnumerable<Device>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Device>>> GetDevicesBySite(
        string siteId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting devices for site {SiteId}", siteId);
            var devices = await _deviceRepository.GetBySiteIdAsync(siteId, cancellationToken);
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving devices for site {SiteId}", siteId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all active devices
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active devices</returns>
    /// <response code="200">Returns active devices</response>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<Device>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Device>>> GetActiveDevices(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting active devices");
            var devices = await _deviceRepository.GetActiveAsync(cancellationToken);
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active devices");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all online devices
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of online devices</returns>
    /// <response code="200">Returns online devices</response>
    [HttpGet("online")]
    [ProducesResponseType(typeof(IEnumerable<Device>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Device>>> GetOnlineDevices(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting online devices");
            var devices = await _deviceRepository.GetOnlineAsync(cancellationToken);
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving online devices");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a new device
    /// </summary>
    /// <param name="request">Device creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created device</returns>
    /// <response code="201">Device created successfully</response>
    /// <response code="400">Invalid device data</response>
    /// <response code="409">Device with same ID already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(Device), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Device>> CreateDevice(
        [FromBody] CreateDeviceRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating device {DeviceId} at site {SiteId}", request.Id, request.SiteId);

            var command = new CreateDeviceCommand(
                request.Id,
                request.Name,
                request.Type,
                request.SiteId,
                request.TopicPattern,
                request.Manufacturer,
                request.Model,
                request.SerialNumber,
                request.Location,
                request.Description
            );

            var device = await _mediator.Send(command, cancellationToken);
            
            return CreatedAtAction(nameof(GetDevice), new { id = device.Id }, device);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating device {DeviceId}", request.Id);
            return ex.Message.Contains("already exists") ? Conflict(ex.Message) : BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device {DeviceId}", request.Id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates an existing device
    /// </summary>
    /// <param name="id">Device identifier</param>
    /// <param name="request">Device update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated device</returns>
    /// <response code="200">Device updated successfully</response>
    /// <response code="400">Invalid device data</response>
    /// <response code="404">Device not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Device>> UpdateDevice(
        string id,
        [FromBody] UpdateDeviceRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating device {DeviceId}", id);

            var command = new UpdateDeviceCommand(
                id,
                request.Name,
                request.Type,
                request.TopicPattern,
                request.Manufacturer,
                request.Model,
                request.SerialNumber,
                request.Location,
                request.Description,
                request.IsActive
            );

            var device = await _mediator.Send(command, cancellationToken);
            
            return Ok(device);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating device {DeviceId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device {DeviceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates device online status
    /// </summary>
    /// <param name="id">Device identifier</param>
    /// <param name="request">Status update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">Status updated successfully</response>
    /// <response code="404">Device not found</response>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateDeviceStatus(
        string id,
        [FromBody] UpdateDeviceStatusRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating status for device {DeviceId} to {IsOnline}", id, request.IsOnline);

            var command = new UpdateDeviceOnlineStatusCommand(id, request.IsOnline, request.LastSeenAt);
            var success = await _mediator.Send(command, cancellationToken);
            
            if (!success)
            {
                return NotFound($"Device with ID '{id}' not found");
            }
            
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for device {DeviceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes a device
    /// </summary>
    /// <param name="id">Device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">Device deleted successfully</response>
    /// <response code="404">Device not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteDevice(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting device {DeviceId}", id);

            var command = new DeleteDeviceCommand(id);
            var success = await _mediator.Send(command, cancellationToken);
            
            if (!success)
            {
                return NotFound($"Device with ID '{id}' not found");
            }
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device {DeviceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Request model for creating a new device
/// </summary>
/// <param name="Id">Unique device identifier</param>
/// <param name="Name">Human-readable device name</param>
/// <param name="Type">Device type (e.g., Battery, SolarPanel, Inverter, Smartmeter, Shelly)</param>
/// <param name="SiteId">Site where the device is located</param>
/// <param name="TopicPattern">Custom MQTT topic pattern for this device (optional)</param>
/// <param name="Manufacturer">Device manufacturer (optional)</param>
/// <param name="Model">Device model (optional)</param>
/// <param name="SerialNumber">Device serial number (optional)</param>
/// <param name="Location">Physical location within the site (optional)</param>
/// <param name="Description">Device description (optional)</param>
public record CreateDeviceRequest(
    string Id,
    string Name,
    DeviceType Type,
    string SiteId,
    string? TopicPattern = null,
    string? Manufacturer = null,
    string? Model = null,
    string? SerialNumber = null,
    string? Location = null,
    string? Description = null
);

/// <summary>
/// Request model for updating a device
/// </summary>
/// <param name="Name">Updated device name</param>
/// <param name="Type">Updated device type</param>
/// <param name="TopicPattern">Custom MQTT topic pattern for this device (optional)</param>
/// <param name="Manufacturer">Updated manufacturer (optional)</param>
/// <param name="Model">Updated model (optional)</param>
/// <param name="SerialNumber">Updated serial number (optional)</param>
/// <param name="Location">Updated location (optional)</param>
/// <param name="Description">Updated description (optional)</param>
/// <param name="IsActive">Whether the device is active</param>
public record UpdateDeviceRequest(
    string Name,
    DeviceType Type,
    string? TopicPattern = null,
    string? Manufacturer = null,
    string? Model = null,
    string? SerialNumber = null,
    string? Location = null,
    string? Description = null,
    bool IsActive = true
);

/// <summary>
/// Request model for updating device status
/// </summary>
/// <param name="IsOnline">Online status</param>
/// <param name="LastSeenAt">Last seen timestamp (optional)</param>
public record UpdateDeviceStatusRequest(
    bool IsOnline,
    DateTime? LastSeenAt = null
);