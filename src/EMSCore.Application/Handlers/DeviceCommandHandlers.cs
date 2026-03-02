using MediatR;
using Microsoft.Extensions.Logging;
using EMSCore.Application.Commands;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Interfaces;

namespace EMSCore.Application.Handlers;

/// <summary>
/// Command handlers for device and site management operations
/// Implements business logic for creating, updating, and deleting devices and sites
/// </summary>
public class DeviceCommandHandlers :
    IRequestHandler<CreateDeviceCommand, Device>,
    IRequestHandler<UpdateDeviceCommand, Device>,
    IRequestHandler<UpdateDeviceOnlineStatusCommand, bool>,
    IRequestHandler<DeleteDeviceCommand, bool>,
    IRequestHandler<CreateSiteCommand, Site>,
    IRequestHandler<UpdateSiteCommand, Site>,
    IRequestHandler<DeleteSiteCommand, bool>,
    IRequestHandler<StoreMeasurementCommand, EnergyMeasurement>,
    IRequestHandler<StoreMeasurementsBatchCommand, int>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly ISiteRepository _siteRepository;
    private readonly IEnergyMeasurementRepository _measurementRepository;
    private readonly ILogger<DeviceCommandHandlers> _logger;

    /// <summary>
    /// Initializes a new instance of the DeviceCommandHandlers
    /// </summary>
    /// <param name="deviceRepository">Repository for device operations</param>
    /// <param name="siteRepository">Repository for site operations</param>
    /// <param name="measurementRepository">Repository for measurement operations</param>
    /// <param name="logger">Logger for handler operations</param>
    public DeviceCommandHandlers(
        IDeviceRepository deviceRepository,
        ISiteRepository siteRepository,
        IEnergyMeasurementRepository measurementRepository,
        ILogger<DeviceCommandHandlers> logger)
    {
        _deviceRepository = deviceRepository;
        _siteRepository = siteRepository;
        _measurementRepository = measurementRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the CreateDeviceCommand
    /// Creates a new device after validating that the site exists
    /// </summary>
    /// <param name="request">Command with device creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created device entity</returns>
    /// <exception cref="InvalidOperationException">Thrown when site doesn't exist or device ID already exists</exception>
    public async Task<Device> Handle(CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating device {DeviceId} at site {SiteId}", request.Id, request.SiteId);

        // Validate that the site exists
        var siteExists = await _siteRepository.ExistsAsync(request.SiteId, cancellationToken);
        if (!siteExists)
        {
            _logger.LogWarning("Attempted to create device {DeviceId} for non-existent site {SiteId}", 
                request.Id, request.SiteId);
            throw new InvalidOperationException($"Site with ID '{request.SiteId}' does not exist");
        }

        // Validate that device ID is unique
        var deviceExists = await _deviceRepository.ExistsAsync(request.Id, cancellationToken);
        if (deviceExists)
        {
            _logger.LogWarning("Attempted to create device with existing ID {DeviceId}", request.Id);
            throw new InvalidOperationException($"Device with ID '{request.Id}' already exists");
        }

        // Create the device entity
        var device = new Device
        {
            Id = request.Id,
            Name = request.Name,
            Type = request.Type,
            TopicPattern = request.TopicPattern,
            SiteId = request.SiteId,
            Manufacturer = request.Manufacturer,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            Location = request.Location,
            Description = request.Description,
            IsActive = true,
            IsOnline = false
        };

        // Save the device
        var createdDevice = await _deviceRepository.AddAsync(device, cancellationToken);
        
        _logger.LogInformation("Successfully created device {DeviceId} of type {DeviceType} at site {SiteId}", 
            createdDevice.Id, createdDevice.Type, createdDevice.SiteId);

        return createdDevice;
    }

    /// <summary>
    /// Handles the UpdateDeviceCommand
    /// Updates an existing device with new information
    /// </summary>
    /// <param name="request">Command with device update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated device entity</returns>
    /// <exception cref="InvalidOperationException">Thrown when device doesn't exist</exception>
    public async Task<Device> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating device {DeviceId}", request.Id);

        // Get the existing device
        var existingDevice = await _deviceRepository.GetByIdAsync(request.Id, cancellationToken);
        if (existingDevice == null)
        {
            _logger.LogWarning("Attempted to update non-existent device {DeviceId}", request.Id);
            throw new InvalidOperationException($"Device with ID '{request.Id}' does not exist");
        }

        // Update the device properties
        existingDevice.Name = request.Name;
        existingDevice.Type = request.Type;
        existingDevice.TopicPattern = request.TopicPattern;
        existingDevice.Manufacturer = request.Manufacturer;
        existingDevice.Model = request.Model;
        existingDevice.SerialNumber = request.SerialNumber;
        existingDevice.Location = request.Location;
        existingDevice.Description = request.Description;
        existingDevice.IsActive = request.IsActive;

        // Save the updated device
        var updatedDevice = await _deviceRepository.UpdateAsync(existingDevice, cancellationToken);
        
        _logger.LogInformation("Successfully updated device {DeviceId}", updatedDevice.Id);

        return updatedDevice;
    }

    /// <summary>
    /// Handles the UpdateDeviceOnlineStatusCommand
    /// Updates the online status and last seen timestamp for a device
    /// </summary>
    /// <param name="request">Command with device status update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if update was successful</returns>
    public async Task<bool> Handle(UpdateDeviceOnlineStatusCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating online status for device {DeviceId} to {IsOnline}", 
            request.DeviceId, request.IsOnline);

        var success = await _deviceRepository.UpdateOnlineStatusAsync(
            request.DeviceId, 
            request.IsOnline, 
            request.LastSeenAt, 
            cancellationToken);

        if (success)
        {
            _logger.LogDebug("Successfully updated online status for device {DeviceId}", request.DeviceId);
        }
        else
        {
            _logger.LogWarning("Failed to update online status for device {DeviceId} - device not found", 
                request.DeviceId);
        }

        return success;
    }

    /// <summary>
    /// Handles the DeleteDeviceCommand
    /// Deletes a device and all its associated measurements
    /// </summary>
    /// <param name="request">Command with device ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful</returns>
    public async Task<bool> Handle(DeleteDeviceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting device {DeviceId}", request.DeviceId);

        var success = await _deviceRepository.DeleteAsync(request.DeviceId, cancellationToken);

        if (success)
        {
            _logger.LogInformation("Successfully deleted device {DeviceId} and all associated data", 
                request.DeviceId);
        }
        else
        {
            _logger.LogWarning("Failed to delete device {DeviceId} - device not found", request.DeviceId);
        }

        return success;
    }

    /// <summary>
    /// Handles the CreateSiteCommand
    /// Creates a new site with the provided information
    /// </summary>
    /// <param name="request">Command with site creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created site entity</returns>
    /// <exception cref="InvalidOperationException">Thrown when site ID already exists</exception>
    public async Task<Site> Handle(CreateSiteCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating site {SiteId} - {SiteName}", request.Id, request.Name);

        // Validate that site ID is unique
        var siteExists = await _siteRepository.ExistsAsync(request.Id, cancellationToken);
        if (siteExists)
        {
            _logger.LogWarning("Attempted to create site with existing ID {SiteId}", request.Id);
            throw new InvalidOperationException($"Site with ID '{request.Id}' already exists");
        }

        // Create the site entity
        var site = new Site
        {
            Id = request.Id,
            Name = request.Name,
            Location = request.Location,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TimeZone = request.TimeZone,
            Description = request.Description,
            IsActive = true
        };

        // Save the site
        var createdSite = await _siteRepository.AddAsync(site, cancellationToken);
        
        _logger.LogInformation("Successfully created site {SiteId} - {SiteName} at {Location}", 
            createdSite.Id, createdSite.Name, createdSite.Location);

        return createdSite;
    }

    /// <summary>
    /// Handles the UpdateSiteCommand
    /// Updates an existing site with new information
    /// </summary>
    /// <param name="request">Command with site update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated site entity</returns>
    /// <exception cref="InvalidOperationException">Thrown when site doesn't exist</exception>
    public async Task<Site> Handle(UpdateSiteCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating site {SiteId}", request.Id);

        // Get the existing site
        var existingSite = await _siteRepository.GetByIdAsync(request.Id, cancellationToken);
        if (existingSite == null)
        {
            _logger.LogWarning("Attempted to update non-existent site {SiteId}", request.Id);
            throw new InvalidOperationException($"Site with ID '{request.Id}' does not exist");
        }

        // Update the site properties
        existingSite.Name = request.Name;
        existingSite.Location = request.Location;
        existingSite.Latitude = request.Latitude;
        existingSite.Longitude = request.Longitude;
        existingSite.TimeZone = request.TimeZone;
        existingSite.Description = request.Description;
        existingSite.IsActive = request.IsActive;

        // Save the updated site
        var updatedSite = await _siteRepository.UpdateAsync(existingSite, cancellationToken);
        
        _logger.LogInformation("Successfully updated site {SiteId} - {SiteName}", 
            updatedSite.Id, updatedSite.Name);

        return updatedSite;
    }

    /// <summary>
    /// Handles the DeleteSiteCommand
    /// Deletes a site and all its associated devices and measurements
    /// </summary>
    /// <param name="request">Command with site ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful</returns>
    public async Task<bool> Handle(DeleteSiteCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting site {SiteId}", request.SiteId);

        var success = await _siteRepository.DeleteAsync(request.SiteId, cancellationToken);

        if (success)
        {
            _logger.LogInformation("Successfully deleted site {SiteId} and all associated data", 
                request.SiteId);
        }
        else
        {
            _logger.LogWarning("Failed to delete site {SiteId} - site not found", request.SiteId);
        }

        return success;
    }

    /// <summary>
    /// Handles the StoreMeasurementCommand
    /// Stores a single energy measurement
    /// </summary>
    /// <param name="request">Command with measurement to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stored measurement entity</returns>
    public async Task<EnergyMeasurement> Handle(StoreMeasurementCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Storing measurement for device {DeviceId}: {Type} = {Value} {Unit}", 
            request.Measurement.DeviceId, request.Measurement.Type, 
            request.Measurement.Value, request.Measurement.Unit);

        var storedMeasurement = await _measurementRepository.AddMeasurementAsync(
            request.Measurement, cancellationToken);

        return storedMeasurement;
    }

    /// <summary>
    /// Handles the StoreMeasurementsBatchCommand
    /// Stores multiple energy measurements in a batch for improved performance
    /// </summary>
    /// <param name="request">Command with measurements to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of measurements stored</returns>
    public async Task<int> Handle(StoreMeasurementsBatchCommand request, CancellationToken cancellationToken)
    {
        var measurementCount = request.Measurements.Count();
        _logger.LogDebug("Storing batch of {Count} measurements", measurementCount);

        var storedCount = await _measurementRepository.AddMeasurementsBatchAsync(
            request.Measurements, cancellationToken);

        _logger.LogDebug("Successfully stored {StoredCount} of {TotalCount} measurements", 
            storedCount, measurementCount);

        return storedCount;
    }
}