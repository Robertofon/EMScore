namespace EMSCore.Domain.Interfaces;

/// <summary>
/// Defines statistics for energy measurements
/// </summary>
public interface IMeasurementStatistics
{
    double Average { get; }
    double Minimum { get; }
    double Maximum { get; }
    double Sum { get; }
    int Count { get; }
    DateTime FirstTimestamp { get; }
    DateTime LastTimestamp { get; }
}
