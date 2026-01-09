using EMSCore.Domain.Entities;
using EMSCore.Domain.Enums;
using DateTimeRange;

namespace EMSCore.Domain.Interfaces;

/// <summary>
/// Repository interface for energy measurement operations
/// </summary>
public interface IEnergyMeasurementRepository
{
    /// <summary>
    /// Gets measurements for a specific device within a time range
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="range">Time range for measurements</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of energy measurements</returns>
    Task<IEnumerable<EnergyMeasurement>> GetMeasurementsAsync(
        string deviceId, 
        DateTimeRange.DateTimeRange range, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets measurements for a specific site within a time range
    /// </summary>
    /// <param name="siteId">Site identifier</param>
    /// <param name="range">Time range for measurements</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of energy measurements</returns>
    Task<IEnumerable<EnergyMeasurement>> GetSiteMeasurementsAsync(
        string siteId, 
        DateTimeRange.DateTimeRange range, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets measurements by type within a time range
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="measurementType">Type of measurement</param>
    /// <param name="range">Time range for measurements</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of energy measurements</returns>
    Task<IEnumerable<EnergyMeasurement>> GetMeasurementsByTypeAsync(
        string deviceId, 
        MeasurementType measurementType, 
        DateTimeRange.DateTimeRange range, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated measurements using TimescaleDB time_bucket function
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="interval">Aggregation interval (e.g., 1 hour, 15 minutes)</param>
    /// <param name="range">Time range for aggregation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of aggregated measurements</returns>
    Task<IEnumerable<EnergyMeasurement>> GetAggregatedDataAsync(
        string deviceId, 
        TimeSpan interval, 
        DateTimeRange.DateTimeRange range, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new energy measurement
    /// </summary>
    /// <param name="measurement">Energy measurement to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Added energy measurement</returns>
    Task<EnergyMeasurement> AddMeasurementAsync(
        EnergyMeasurement measurement, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple energy measurements in batch
    /// </summary>
    /// <param name="measurements">Collection of measurements to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of measurements added</returns>
    Task<int> AddMeasurementsBatchAsync(
        IEnumerable<EnergyMeasurement> measurements, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest measurement for a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="measurementType">Optional measurement type filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest measurement or null if none found</returns>
    Task<EnergyMeasurement?> GetLatestMeasurementAsync(
        string deviceId, 
        MeasurementType? measurementType = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets measurement statistics for a device and time range
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="measurementType">Type of measurement</param>
    /// <param name="range">Time range for statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Measurement statistics</returns>
    Task<MeasurementStatistics> GetMeasurementStatisticsAsync(
        string deviceId, 
        MeasurementType measurementType, 
        DateTimeRange.DateTimeRange range, 
        CancellationToken cancellationToken = default);
}

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
);