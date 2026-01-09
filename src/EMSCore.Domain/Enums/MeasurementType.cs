namespace EMSCore.Domain.Enums;

/// <summary>
/// Defines the types of energy measurements that can be recorded
/// </summary>
public enum MeasurementType
{
    /// <summary>
    /// Voltage measurement in Volts (V)
    /// </summary>
    Voltage = 0,
    
    /// <summary>
    /// Current measurement in Amperes (A)
    /// </summary>
    Current = 1,
    
    /// <summary>
    /// Power measurement in Watts (W)
    /// </summary>
    Power = 2,
    
    /// <summary>
    /// Frequency measurement in Hertz (Hz)
    /// </summary>
    Frequency = 3,
    
    /// <summary>
    /// Temperature measurement in Celsius (°C)
    /// </summary>
    Temperature = 4,
    
    /// <summary>
    /// Battery State of Charge in percentage (%)
    /// </summary>
    BatterySOC = 5,
    
    /// <summary>
    /// Battery voltage in Volts (V)
    /// </summary>
    BatteryVoltage = 6,
    
    /// <summary>
    /// Solar irradiance in W/m²
    /// </summary>
    SolarIrradiance = 7,
    
    /// <summary>
    /// Energy consumption in kWh
    /// </summary>
    EnergyConsumption = 8,
    
    /// <summary>
    /// Energy production in kWh
    /// </summary>
    EnergyProduction = 9
}