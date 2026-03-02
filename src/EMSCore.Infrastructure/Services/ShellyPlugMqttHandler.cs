using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Enums;
using EMSCore.Domain.Interfaces;

namespace EMSCore.Infrastructure.Services;

public class ShellyPlugMqttHandler
{
    private readonly ILogger<ShellyPlugMqttHandler> _logger;
    private readonly IEnergyMeasurementRepository _measurementRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ISiteRepository _siteRepository;
    private readonly DeviceTypeTopicMapper _topicMapper;
    private readonly ShellyConfiguration _config;

    public ShellyPlugMqttHandler(
        ILogger<ShellyPlugMqttHandler> logger,
        IEnergyMeasurementRepository measurementRepository,
        IDeviceRepository deviceRepository,
        ISiteRepository siteRepository,
        DeviceTypeTopicMapper topicMapper,
        IOptions<ShellyConfiguration> config)
    {
        _logger = logger;
        _measurementRepository = measurementRepository;
        _deviceRepository = deviceRepository;
        _siteRepository = siteRepository;
        _topicMapper = topicMapper;
        _config = config.Value;
    }

    public async Task HandleShellyPowerAsync(MqttMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Processing Shelly power message from topic: {Topic}", message.Topic);

            var topicParts = message.Topic.Split('/');
            if (topicParts.Length < 4 || topicParts[0] != "shellies")
            {
                _logger.LogWarning("Invalid Shelly topic format: {Topic}", message.Topic);
                return;
            }

            var deviceId = topicParts[1];
            var measurementType = topicParts[3];
            var siteId = _config.DefaultSiteId;

            var device = await GetOrCreateDeviceAsync(deviceId, siteId, cancellationToken);
            if (device == null)
            {
                return;
            }

            await _deviceRepository.UpdateOnlineStatusAsync(deviceId, true, DateTime.UtcNow, cancellationToken);

            var timestamp = DateTime.UtcNow;

            if (measurementType == "power")
            {
                var powerValue = JsonSerializer.Deserialize<ShellyPowerPayload>(message.PayloadAsString, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (powerValue != null && IsValidPower(powerValue.Power))
                {
                    var measurement = new EnergyMeasurement
                    {
                        Timestamp = timestamp,
                        DeviceId = deviceId,
                        SiteId = siteId,
                        Type = MeasurementType.Power,
                        Value = powerValue.Power,
                        Unit = "W",
                        Quality = QualityFlag.Good,
                        AggregationLevel = "raw"
                    };

                    await _measurementRepository.AddMeasurementAsync(measurement, cancellationToken);
                    _logger.LogDebug("Stored power measurement for {DeviceId}: {Power} W", deviceId, powerValue.Power);
                }
                else if (powerValue != null)
                {
                    _logger.LogWarning("Invalid power value for {DeviceId}: {Power} W", deviceId, powerValue.Power);
                }
            }
            else if (measurementType == "energy")
            {
                var energyValue = JsonSerializer.Deserialize<ShellyEnergyPayload>(message.PayloadAsString, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (energyValue != null && IsValidEnergy(energyValue.Energy))
                {
                    var measurement = new EnergyMeasurement
                    {
                        Timestamp = timestamp,
                        DeviceId = deviceId,
                        SiteId = siteId,
                        Type = MeasurementType.EnergyConsumption,
                        Value = energyValue.Energy / 1000.0,
                        Unit = "kWh",
                        Quality = QualityFlag.Good,
                        AggregationLevel = "raw"
                    };

                    await _measurementRepository.AddMeasurementAsync(measurement, cancellationToken);
                    _logger.LogDebug("Stored energy measurement for {DeviceId}: {Energy} kWh", deviceId, energyValue.Energy / 1000.0);
                }
                else if (energyValue != null)
                {
                    _logger.LogWarning("Invalid energy value for {DeviceId}: {Energy} Wh", deviceId, energyValue.Energy);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Shelly JSON payload from topic {Topic}", message.Topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Shelly message from topic {Topic}", message.Topic);
        }
    }

    public async Task HandleShellyRelayPowerAsync(MqttMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Processing Shelly relay power message from topic: {Topic}", message.Topic);

            var topicParts = message.Topic.Split('/');
            if (topicParts.Length < 5 || topicParts[0] != "shellies" || topicParts[3] != "relay")
            {
                _logger.LogWarning("Invalid Shelly relay topic format: {Topic}", message.Topic);
                return;
            }

            var deviceId = $"{topicParts[1]}-relay-{topicParts[4]}";
            var measurementType = topicParts[5];
            var siteId = _config.DefaultSiteId;

            var device = await GetOrCreateDeviceAsync(deviceId, siteId, cancellationToken);
            if (device == null)
            {
                return;
            }

            await _deviceRepository.UpdateOnlineStatusAsync(deviceId, true, DateTime.UtcNow, cancellationToken);

            var timestamp = DateTime.UtcNow;

            if (measurementType == "power")
            {
                var powerValue = JsonSerializer.Deserialize<ShellyPowerPayload>(message.PayloadAsString, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (powerValue != null && IsValidPower(powerValue.Power))
                {
                    var measurement = new EnergyMeasurement
                    {
                        Timestamp = timestamp,
                        DeviceId = deviceId,
                        SiteId = siteId,
                        Type = MeasurementType.Power,
                        Value = powerValue.Power,
                        Unit = "W",
                        Quality = QualityFlag.Good,
                        AggregationLevel = "raw"
                    };

                    await _measurementRepository.AddMeasurementAsync(measurement, cancellationToken);
                    _logger.LogDebug("Stored relay power measurement for {DeviceId}: {Power} W", deviceId, powerValue.Power);
                }
                else if (powerValue != null)
                {
                    _logger.LogWarning("Invalid relay power value for {DeviceId}: {Power} W", deviceId, powerValue.Power);
                }
            }
            else if (measurementType == "energy")
            {
                var energyValue = JsonSerializer.Deserialize<ShellyEnergyPayload>(message.PayloadAsString, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (energyValue != null && IsValidEnergy(energyValue.Energy))
                {
                    var measurement = new EnergyMeasurement
                    {
                        Timestamp = timestamp,
                        DeviceId = deviceId,
                        SiteId = siteId,
                        Type = MeasurementType.EnergyConsumption,
                        Value = energyValue.Energy / 1000.0,
                        Unit = "kWh",
                        Quality = QualityFlag.Good,
                        AggregationLevel = "raw"
                    };

                    await _measurementRepository.AddMeasurementAsync(measurement, cancellationToken);
                    _logger.LogDebug("Stored relay energy measurement for {DeviceId}: {Energy} kWh", deviceId, energyValue.Energy / 1000.0);
                }
                else if (energyValue != null)
                {
                    _logger.LogWarning("Invalid relay energy value for {DeviceId}: {Energy} Wh", deviceId, energyValue.Energy);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Shelly relay JSON payload from topic {Topic}", message.Topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Shelly relay message from topic {Topic}", message.Topic);
        }
    }

    private async Task<Device?> GetOrCreateDeviceAsync(string deviceId, string siteId, CancellationToken cancellationToken)
    {
        var existingDevice = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken);
        if (existingDevice != null)
        {
            return existingDevice;
        }

        var siteExists = await _siteRepository.ExistsAsync(siteId, cancellationToken);
        if (!siteExists)
        {
            _logger.LogWarning("Site {SiteId} does not exist, cannot create implicit Shelly device", siteId);
            return null;
        }

        var device = new Device
        {
            Id = deviceId,
            Name = $"Shelly {deviceId}",
            Type = DeviceType.Shelly,
            TopicPattern = _topicMapper.GetTopicConfig(DeviceType.Shelly).MeasurementTopic,
            SiteId = siteId,
            IsActive = true,
            IsOnline = false
        };

        await _deviceRepository.AddAsync(device, cancellationToken);
        _logger.LogInformation("Created implicit Shelly device: {DeviceId} at site {SiteId}", deviceId, siteId);

        return device;
    }

    private static bool IsValidPower(double power)
    {
        return power >= 0 && power <= 100000;
    }

    private static bool IsValidEnergy(double energy)
    {
        return energy >= 0 && energy <= 1000000000;
    }
}

public class ShellyConfiguration
{
    public string DefaultSiteId { get; set; } = "default";
}

public record ShellyPowerPayload(double Power);

public record ShellyEnergyPayload(double Energy);
