namespace EMSCore.Domain.Enums;

/// <summary>
/// Represents the state of an actuator (relay, switch, etc.)
/// </summary>
public enum ActuatorState
{
    /// <summary>State is unknown or not initialized</summary>
    Unknown = 0,
    
    /// <summary>Actuator is turned off</summary>
    Off = 1,
    
    /// <summary>Actuator is turned on</summary>
    On = 2
}
