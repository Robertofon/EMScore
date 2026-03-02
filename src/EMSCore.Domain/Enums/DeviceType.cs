namespace EMSCore.Domain.Enums;

/// <summary>
/// Defines the types of devices supported by the EMS
/// </summary>
public enum DeviceType
{
    /// <summary>
    /// Generic or unspecified device type
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// Battery energy storage system
    /// </summary>
    Battery = 1,
    
    /// <summary>
    /// Solar panel or PV system
    /// </summary>
    SolarPanel = 2,
    
    /// <summary>
    /// Inverter for converting DC to AC
    /// </summary>
    Inverter = 3,
    
    /// <summary>
    /// Generic sensor device
    /// </summary>
    Sensor = 4,
    
    /// <summary>
    /// Smart meter for energy consumption/production tracking
    /// </summary>
    Smartmeter = 5,
    
    /// <summary>
    /// Shelly smart home device
    /// </summary>
    Shelly = 6,
    
    /// <summary>
    /// Electric vehicle charger
    /// </summary>
    EvCharger = 7,
    
    /// <summary>
    /// Heat pump system
    /// </summary>
    HeatPump = 8,
    
    /// <summary>
    /// Wind turbine
    /// </summary>
    WindTurbine = 9,
    
    /// <summary>
    /// Generator or backup power system
    /// </summary>
    Generator = 10,
    
    /// <summary>
    /// Smart home controller or hub
    /// </summary>
    Hub = 11,
    
    /// <summary>
    /// Load controller or demand response device
    /// </summary>
    LoadController = 12
}
