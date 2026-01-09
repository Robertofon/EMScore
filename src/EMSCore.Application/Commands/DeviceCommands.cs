using MediatR;
using EMSCore.Domain.Entities;

namespace EMSCore.Application.Commands;

/// <summary>
/// Command to create a new device
/// </summary>
/// <param name="Id">Unique device identifier</param>
/// <param name="Name">Human-readable device name</param>
/// <param name="Type">Device type (e.g., "Battery", "Solar Panel", "Inverter")</param>
/// <param name="SiteId">Site where the device is located</param>
/// <param name="Manufacturer">Device manufacturer (optional)</param>
/// <param name="Model">Device model (optional)</param>
/// <param name="SerialNumber">Device serial number (optional)</param>
/// <param name="Location">Physical location within the site (optional)</param>
/// <param name="Description">Device description (optional)</param>
public record CreateDeviceCommand(
    string Id,
    string Name,
    string Type,
    string SiteId,
    string? Manufacturer = null,
    string? Model = null,
    string? SerialNumber = null,
    string? Location = null,
    string? Description = null
) : IRequest<Device>;

/// <summary>
/// Command to update an existing device
/// </summary>
/// <param name="Id">Device identifier</param>
/// <param name="Name">Updated device name</param>
/// <param name="Type">Updated device type</param>
/// <param name="Manufacturer">Updated manufacturer (optional)</param>
/// <param name="Model">Updated model (optional)</param>
/// <param name="SerialNumber">Updated serial number (optional)</param>
/// <param name="Location">Updated location (optional)</param>
/// <param name="Description">Updated description (optional)</param>
/// <param name="IsActive">Whether the device is active</param>
public record UpdateDeviceCommand(
    string Id,
    string Name,
    string Type,
    string? Manufacturer = null,
    string? Model = null,
    string? SerialNumber = null,
    string? Location = null,
    string? Description = null,
    bool IsActive = true
) : IRequest<Device>;

/// <summary>
/// Command to update device online status
/// </summary>
/// <param name="DeviceId">Device identifier</param>
/// <param name="IsOnline">Online status</param>
/// <param name="LastSeenAt">Last seen timestamp (optional, defaults to now)</param>
public record UpdateDeviceOnlineStatusCommand(
    string DeviceId,
    bool IsOnline,
    DateTime? LastSeenAt = null
) : IRequest<bool>;

/// <summary>
/// Command to delete a device
/// </summary>
/// <param name="DeviceId">Device identifier to delete</param>
public record DeleteDeviceCommand(string DeviceId) : IRequest<bool>;

/// <summary>
/// Command to create a new site
/// </summary>
/// <param name="Id">Unique site identifier</param>
/// <param name="Name">Human-readable site name</param>
/// <param name="Location">Physical location description</param>
/// <param name="Latitude">GPS latitude (optional)</param>
/// <param name="Longitude">GPS longitude (optional)</param>
/// <param name="TimeZone">Site timezone (defaults to UTC)</param>
/// <param name="Description">Site description (optional)</param>
public record CreateSiteCommand(
    string Id,
    string Name,
    string Location,
    double? Latitude = null,
    double? Longitude = null,
    string TimeZone = "UTC",
    string? Description = null
) : IRequest<Site>;

/// <summary>
/// Command to update an existing site
/// </summary>
/// <param name="Id">Site identifier</param>
/// <param name="Name">Updated site name</param>
/// <param name="Location">Updated location description</param>
/// <param name="Latitude">Updated GPS latitude (optional)</param>
/// <param name="Longitude">Updated GPS longitude (optional)</param>
/// <param name="TimeZone">Updated timezone</param>
/// <param name="Description">Updated description (optional)</param>
/// <param name="IsActive">Whether the site is active</param>
public record UpdateSiteCommand(
    string Id,
    string Name,
    string Location,
    double? Latitude = null,
    double? Longitude = null,
    string TimeZone = "UTC",
    string? Description = null,
    bool IsActive = true
) : IRequest<Site>;

/// <summary>
/// Command to delete a site
/// </summary>
/// <param name="SiteId">Site identifier to delete</param>
public record DeleteSiteCommand(string SiteId) : IRequest<bool>;

/// <summary>
/// Command to store a single energy measurement
/// </summary>
/// <param name="Measurement">Energy measurement to store</param>
public record StoreMeasurementCommand(EnergyMeasurement Measurement) : IRequest<EnergyMeasurement>;

/// <summary>
/// Command to store multiple energy measurements in batch
/// </summary>
/// <param name="Measurements">Collection of measurements to store</param>
public record StoreMeasurementsBatchCommand(IEnumerable<EnergyMeasurement> Measurements) : IRequest<int>;