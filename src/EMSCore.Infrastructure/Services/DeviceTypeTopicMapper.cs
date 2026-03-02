using EMSCore.Domain.Enums;

namespace EMSCore.Infrastructure.Services;

/// <summary>
/// Provides topic mapping configuration for different device types
/// </summary>
public class DeviceTypeTopicMapper
{
    private readonly Dictionary<DeviceType, DeviceTopicConfig> _defaultMappings;

    public DeviceTypeTopicMapper()
    {
        _defaultMappings = new Dictionary<DeviceType, DeviceTopicConfig>
        {
            [DeviceType.Unknown] = new DeviceTopicConfig(
                MeasurementTopic: "ems/{site_id}/devices/{device_id}/measurements/+",
                StatusTopic: "ems/{site_id}/devices/{device_id}/status",
                BatchTopic: "ems/{site_id}/devices/{device_id}/measurements/batch"
            ),
            [DeviceType.Battery] = new DeviceTopicConfig(
                MeasurementTopic: "ems/{site_id}/devices/{device_id}/measurements/+",
                StatusTopic: "ems/{site_id}/devices/{device_id}/status",
                BatchTopic: "ems/{site_id}/devices/{device_id}/measurements/batch"
            ),
            [DeviceType.SolarPanel] = new DeviceTopicConfig(
                MeasurementTopic: "solar/{site_id}/panels/{device_id}/measurements/+",
                StatusTopic: "solar/{site_id}/panels/{device_id}/status",
                BatchTopic: "solar/{site_id}/panels/{device_id}/measurements/batch"
            ),
            [DeviceType.Inverter] = new DeviceTopicConfig(
                MeasurementTopic: "inverter/{site_id}/{device_id}/measurements/+",
                StatusTopic: "inverter/{site_id}/{device_id}/status",
                BatchTopic: "inverter/{site_id}/{device_id}/measurements/batch"
            ),
            [DeviceType.Sensor] = new DeviceTopicConfig(
                MeasurementTopic: "sensor/{site_id}/{device_id}/measurements/+",
                StatusTopic: "sensor/{site_id}/{device_id}/status",
                BatchTopic: "sensor/{site_id}/{device_id}/measurements/batch"
            ),
            [DeviceType.Smartmeter] = new DeviceTopicConfig(
                MeasurementTopic: "smartmeter/{site_id}/{device_id}/measurements/+",
                StatusTopic: "smartmeter/{site_id}/{device_id}/status",
                BatchTopic: "smartmeter/{site_id}/{device_id}/measurements/batch"
            ),
            [DeviceType.Shelly] = new DeviceTopicConfig(
                MeasurementTopic: "shelly/{site_id}/{device_id}/measurements/+",
                StatusTopic: "shelly/{site_id}/{device_id}/status",
                BatchTopic: "shelly/{site_id}/{device_id}/measurements/batch"
            ),
            [DeviceType.EvCharger] = new DeviceTopicConfig(
                MeasurementTopic: "evcharger/{site_id}/{device_id}/measurements/+",
                StatusTopic: "evcharger/{site_id}/{device_id}/status",
                BatchTopic: "evcharger/{site_id}/{device_id}/measurements/batch"
            ),
            [DeviceType.HeatPump] = new DeviceTopicConfig(
                MeasurementTopic: "heatpump/{site_id}/{device_id}/measurements/+",
                StatusTopic: "heatpump/{site_id}/{device_id}/status",
                BatchTopic: "heatpump/{site_id}/{device_id}/measurements/batch"
            ),
            [DeviceType.WindTurbine] = new DeviceTopicConfig(
                MeasurementTopic: "wind/{site_id}/turbines/{device_id}/measurements/+",
                StatusTopic: "wind/{site_id}/turbines/{device_id}/status",
                BatchTopic: "wind/{site_id}/turbines/{device_id}/measurements/batch"
            ),
            [DeviceType.Generator] = new DeviceTopicConfig(
                MeasurementTopic: "generator/{site_id}/{device_id}/measurements/+",
                StatusTopic: "generator/{site_id}/{device_id}/status",
                BatchTopic: "generator/{site_id}/{device_id}/measurements/batch"
            ),
            [DeviceType.Hub] = new DeviceTopicConfig(
                MeasurementTopic: "hub/{site_id}/{device_id}/measurements/+",
                StatusTopic: "hub/{site_id}/{device_id}/status",
                BatchTopic: "hub/{site_id}/{device_id}/measurements/batch"
            ),
            [DeviceType.LoadController] = new DeviceTopicConfig(
                MeasurementTopic: "loadctrl/{site_id}/{device_id}/measurements/+",
                StatusTopic: "loadctrl/{site_id}/{device_id}/status",
                BatchTopic: "loadctrl/{site_id}/{device_id}/measurements/batch"
            )
        };
    }

    /// <summary>
    /// Gets the topic configuration for a specific device type
    /// </summary>
    /// <param name="deviceType">The device type</param>
    /// <returns>Topic configuration for the device type</returns>
    public DeviceTopicConfig GetTopicConfig(DeviceType deviceType)
    {
        return _defaultMappings.GetValueOrDefault(deviceType, _defaultMappings[DeviceType.Unknown]);
    }

    /// <summary>
    /// Gets all supported device types
    /// </summary>
    public IEnumerable<DeviceType> GetSupportedDeviceTypes()
    {
        return _defaultMappings.Keys;
    }
}

/// <summary>
/// Configuration for MQTT topics associated with a device type
/// </summary>
public record DeviceTopicConfig(
    string MeasurementTopic,
    string StatusTopic,
    string BatchTopic
);
