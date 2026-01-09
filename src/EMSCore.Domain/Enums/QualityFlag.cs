namespace EMSCore.Domain.Enums;

/// <summary>
/// Indicates the quality of a measurement reading
/// </summary>
public enum QualityFlag
{
    /// <summary>
    /// Good quality measurement - reliable and accurate
    /// </summary>
    Good = 0,
    
    /// <summary>
    /// Uncertain quality - measurement may be inaccurate
    /// </summary>
    Uncertain = 1,
    
    /// <summary>
    /// Bad quality - measurement is unreliable or corrupted
    /// </summary>
    Bad = 2,
    
    /// <summary>
    /// Measurement is missing or not available
    /// </summary>
    Missing = 3,
    
    /// <summary>
    /// Measurement is out of expected range
    /// </summary>
    OutOfRange = 4
}