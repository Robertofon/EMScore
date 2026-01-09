using System.Text.Json;
using Microsoft.Extensions.Logging;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Enums;
using EMSCore.Domain.Interfaces;

namespace EMSCore.Infrastructure.Services;

/// <summary>
/// MQTT message handler specifically for energy measurement data
/// Processes incoming sensor data and stores it in the database
/// </summary>
public class EnergyDataMqttHandler
{
    private readonly ILogger<EnergyDataMqttHandler> _logger;
    private readonly IEnergyMeasurementRepository _measurementRepository;
    private readonly IDeviceRepository _deviceRepository;

    /// <summary>
    /// Initializes a new instance of the EnergyDataMqttHandler
    /// </summary>
    /// <param name="logger">Logger for handler operations</param>
    /// <param name="measurementRepository">Repository for storing measurements</param>
    /// <param name="deviceRepository">Repository for device operations</param>
    public EnergyDataMqttHandler(
        ILogger<EnergyDataMqttHandler> logger,
        IEnergyMeasurementRepository measurementRepository,
        IDeviceRepository deviceRepository)
    {
        _logger = logger;
        _measurementRepository = measurementRepository;
        _deviceRepository = deviceRepository;
    }

    /// <summary>
    /// Handles energy measurement messages from MQTT
    /// Expected topic format: ems/{site_id}/devices/{device_id}/measurements/{type}
    /// </summary>
    /// <param name="message">MQTT message containing energy data</param>
    public async Task HandleEnergyMeasurementAsync(MqttMessage message)
    {
        try
        {
            _logger.LogDebug("Processing energy measurement from topic: {Topic}", message.Topic);

            // Parse topic to extract site_id, device_id, and measurement type
            var topicParts = message.Topic.Split('/');
            if (topicParts.Length < 6 || topicParts[0] != "ems" || topicParts[2] != "devices" || topicParts[4] != "measurements")
            {
                _logger.LogWarning("Invalid topic format for energy measurement: {Topic}", message.Topic);
                return;
            }

            var siteId = topicParts[1];
            var deviceId = topicParts[3];
            var measurementTypeString = topicParts[5];

            // Validate device exists and update last seen timestamp
            var deviceExists = await _deviceRepository.ExistsAsync(deviceId);
            if (!deviceExists)
            {
                _logger.LogWarning("Received measurement from unknown device: {DeviceId}", deviceId);
                return;
            }

            // Update device online status
            await _deviceRepository.UpdateOnlineStatusAsync(deviceId, true, DateTime.UtcNow);

            // Parse measurement type
            if (!Enum.TryParse<MeasurementType>(measurementTypeString, true, out var measurementType))
            {
                _logger.LogWarning("Unknown measurement type: {Type} from device {DeviceId}", measurementTypeString, deviceId);
                return;
            }

            // Deserialize the payload
            var measurementData = JsonSerializer.Deserialize<EnergyMeasurementPayload>(message.PayloadAsString, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (measurementData == null)
            {
                _logger.LogWarning("Failed to deserialize measurement payload from device {DeviceId}", deviceId);
                return;
            }

            // Create energy measurement entity
            var measurement = new EnergyMeasurement
            {
                Timestamp = measurementData.Timestamp ?? DateTime.UtcNow,
                DeviceId = deviceId,
                SiteId = siteId,
                Type = measurementType,
                Value = measurementData.Value,
                Unit = measurementData.Unit ?? GetDefaultUnit(measurementType),
                Quality = measurementData.Quality ?? QualityFlag.Good,
                Phase = measurementData.Phase,
                Metadata = measurementData.Metadata != null ? JsonSerializer.Serialize(measurementData.Metadata) : null,
                AggregationLevel = "raw"
            };

            // Validate measurement data
            if (!IsValidMeasurement(measurement))
            {
                _logger.LogWarning("Invalid measurement data from device {DeviceId}: {Value} {Unit}", 
                    deviceId, measurement.Value, measurement.Unit);
                return;
            }

            // Store the measurement
            await _measurementRepository.AddMeasurementAsync(measurement);

            _logger.LogDebug("Successfully stored measurement from device {DeviceId}: {Type} = {Value} {Unit}", 
                deviceId, measurementType, measurement.Value, measurement.Unit);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON payload from topic {Topic}", message.Topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing energy measurement from topic {Topic}", message.Topic);
        }
    }

    /// <summary>
    /// Handles device status messages from MQTT
    /// Expected topic format: ems/{site_id}/devices/{device_id}/status
    /// </summary>
    /// <param name="message">MQTT message containing device status</param>
    public async Task HandleDeviceStatusAsync(MqttMessage message)
    {
        try
        {
            _logger.LogDebug("Processing device status from topic: {Topic}", message.Topic);

            // Parse topic to extract site_id and device_id
            var topicParts = message.Topic.Split('/');
            if (topicParts.Length < 5 || topicParts[0] != "ems" || topicParts[2] != "devices" || topicParts[4] != "status")
            {
                _logger.LogWarning("Invalid topic format for device status: {Topic}", message.Topic);
                return;
            }

            var deviceId = topicParts[3];

            // Deserialize the payload
            var statusData = JsonSerializer.Deserialize<DeviceStatusPayload>(message.PayloadAsString, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (statusData == null)
            {
                _logger.LogWarning("Failed to deserialize status payload from device {DeviceId}", deviceId);
                return;
            }

            // Update device status
            await _deviceRepository.UpdateOnlineStatusAsync(deviceId, statusData.IsOnline, statusData.Timestamp ?? DateTime.UtcNow);

            _logger.LogDebug("Updated status for device {DeviceId}: Online = {IsOnline}", deviceId, statusData.IsOnline);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON payload from topic {Topic}", message.Topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing device status from topic {Topic}", message.Topic);
        }
    }

    /// <summary>
    /// Handles batch energy measurements for improved performance
    /// Expected topic format: ems/{site_id}/devices/{device_id}/measurements/batch
    /// </summary>
    /// <param name="message">MQTT message containing batch of measurements</param>
    public async Task HandleBatchMeasurementsAsync(MqttMessage message)
    {
        try
        {
            _logger.LogDebug("Processing batch measurements from topic: {Topic}", message.Topic);

            // Parse topic to extract site_id and device_id
            var topicParts = message.Topic.Split('/');
            if (topicParts.Length < 6 || topicParts[0] != "ems" || topicParts[2] != "devices" || topicParts[4] != "measurements" || topicParts[5] != "batch")
            {
                _logger.LogWarning("Invalid topic format for batch measurements: {Topic}", message.Topic);
                return;
            }

            var siteId = topicParts[1];
            var deviceId = topicParts[3];

            // Validate device exists
            var deviceExists = await _deviceRepository.ExistsAsync(deviceId);
            if (!deviceExists)
            {
                _logger.LogWarning("Received batch measurements from unknown device: {DeviceId}", deviceId);
                return;
            }

            // Update device online status
            await _deviceRepository.UpdateOnlineStatusAsync(deviceId, true, DateTime.UtcNow);

            // Deserialize the batch payload
            var batchData = JsonSerializer.Deserialize<BatchMeasurementPayload>(message.PayloadAsString, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (batchData?.Measurements == null || !batchData.Measurements.Any())
            {
                _logger.LogWarning("Empty or invalid batch payload from device {DeviceId}", deviceId);
                return;
            }

            // Convert to energy measurement entities
            var measurements = new List<EnergyMeasurement>();
            foreach (var item in batchData.Measurements)
            {
                if (!Enum.TryParse<MeasurementType>(item.Type, true, out var measurementType))
                {
                    _logger.LogWarning("Unknown measurement type in batch: {Type} from device {DeviceId}", item.Type, deviceId);
                    continue;
                }

                var measurement = new EnergyMeasurement
                {
                    Timestamp = item.Timestamp ?? DateTime.UtcNow,
                    DeviceId = deviceId,
                    SiteId = siteId,
                    Type = measurementType,
                    Value = item.Value,
                    Unit = item.Unit ?? GetDefaultUnit(measurementType),
                    Quality = item.Quality ?? QualityFlag.Good,
                    Phase = item.Phase,
                    Metadata = item.Metadata != null ? JsonSerializer.Serialize(item.Metadata) : null,
                    AggregationLevel = "raw"
                };

                if (IsValidMeasurement(measurement))
                {
                    measurements.Add(measurement);
                }
            }

            if (measurements.Any())
            {
                // Store measurements in batch for better performance
                var storedCount = await _measurementRepository.AddMeasurementsBatchAsync(measurements);
                _logger.LogDebug("Successfully stored {Count} measurements from device {DeviceId}", storedCount, deviceId);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON batch payload from topic {Topic}", message.Topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch measurements from topic {Topic}", message.Topic);
        }
    }

    /// <summary>
    /// Validates measurement data for basic sanity checks
    /// </summary>
    /// <param name="measurement">Measurement to validate</param>
    /// <returns>True if measurement is valid</returns>
    private static bool IsValidMeasurement(EnergyMeasurement measurement)
    {
        // Check for reasonable timestamp (not too far in past or future)
        var now = DateTime.UtcNow;
        if (measurement.Timestamp < now.AddDays(-7) || measurement.Timestamp > now.AddMinutes(5))
        {
            return false;
        }

        // Check for reasonable values based on measurement type
        return measurement.Type switch
        {
            MeasurementType.Voltage => measurement.Value >= 0 && measurement.Value <= 1000, // 0-1000V
            MeasurementType.Current => measurement.Value >= 0 && measurement.Value <= 1000, // 0-1000A
            MeasurementType.Power => measurement.Value >= -100000 && measurement.Value <= 100000, // -100kW to +100kW
            MeasurementType.Frequency => measurement.Value >= 45 && measurement.Value <= 65, // 45-65Hz
            MeasurementType.Temperature => measurement.Value >= -50 && measurement.Value <= 100, // -50°C to 100°C
            MeasurementType.BatterySOC => measurement.Value >= 0 && measurement.Value <= 100, // 0-100%
            MeasurementType.BatteryVoltage => measurement.Value >= 0 && measurement.Value <= 1000, // 0-1000V
            MeasurementType.SolarIrradiance => measurement.Value >= 0 && measurement.Value <= 2000, // 0-2000 W/m²
            MeasurementType.EnergyConsumption => measurement.Value >= 0, // Non-negative
            MeasurementType.EnergyProduction => measurement.Value >= 0, // Non-negative
            _ => true // Unknown types pass validation
        };
    }

    /// <summary>
    /// Gets the default unit for a measurement type
    /// </summary>
    /// <param name="type">Measurement type</param>
    /// <returns>Default unit string</returns>
    private static string GetDefaultUnit(MeasurementType type)
    {
        return type switch
        {
            MeasurementType.Voltage => "V",
            MeasurementType.Current => "A",
            MeasurementType.Power => "W",
            MeasurementType.Frequency => "Hz",
            MeasurementType.Temperature => "°C",
            MeasurementType.BatterySOC => "%",
            MeasurementType.BatteryVoltage => "V",
            MeasurementType.SolarIrradiance => "W/m²",
            MeasurementType.EnergyConsumption => "kWh",
            MeasurementType.EnergyProduction => "kWh",
            _ => ""
        };
    }
}

/// <summary>
/// Payload structure for single energy measurements
/// </summary>
public record EnergyMeasurementPayload(
    double Value,
    string? Unit = null,
    DateTime? Timestamp = null,
    QualityFlag? Quality = null,
    string? Phase = null,
    Dictionary<string, object>? Metadata = null
);

/// <summary>
/// Payload structure for device status updates
/// </summary>
public record DeviceStatusPayload(
    bool IsOnline,
    DateTime? Timestamp = null,
    string? Status = null,
    Dictionary<string, object>? Metadata = null
);

/// <summary>
/// Payload structure for batch measurements
/// </summary>
public record BatchMeasurementPayload(
    IEnumerable<BatchMeasurementItem> Measurements,
    DateTime? Timestamp = null
);

/// <summary>
/// Individual measurement item in a batch
/// </summary>
public record BatchMeasurementItem(
    string Type,
    double Value,
    string? Unit = null,
    DateTime? Timestamp = null,
    QualityFlag? Quality = null,
    string? Phase = null,
    Dictionary<string, object>? Metadata = null
);