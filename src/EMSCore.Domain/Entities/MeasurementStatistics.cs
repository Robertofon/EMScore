using EMSCore.Domain.Interfaces;

namespace EMSCore.Domain.Entities;

/// <summary>
/// Statistics for energy measurements
/// </summary>
public record MeasurementStatistics(
    double Average,
    double Minimum,
    double Maximum,
    double Sum,
    int Count,
    DateTime FirstTimestamp,
    DateTime LastTimestamp
) : IMeasurementStatistics;
