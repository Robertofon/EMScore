using MediatR;
using EMSCore.Application.Queries;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Interfaces;

namespace EMSCore.Application.Handlers;

/// <summary>
/// Handler for energy measurement queries
/// Implements the query handlers for retrieving energy data
/// </summary>
public class EnergyMeasurementQueryHandlers :
    IRequestHandler<GetEnergyMeasurementsQuery, IEnumerable<EnergyMeasurement>>,
    IRequestHandler<GetAggregatedEnergyDataQuery, IEnumerable<EnergyMeasurement>>,
    IRequestHandler<GetSiteEnergyMeasurementsQuery, IEnumerable<EnergyMeasurement>>,
    IRequestHandler<GetLatestMeasurementQuery, EnergyMeasurement?>,
    IRequestHandler<GetMeasurementStatisticsQuery, IMeasurementStatistics>
{
    private readonly IEnergyMeasurementRepository _measurementRepository;

    /// <summary>
    /// Initializes a new instance of the EnergyMeasurementQueryHandlers
    /// </summary>
    /// <param name="measurementRepository">Repository for energy measurements</param>
    public EnergyMeasurementQueryHandlers(IEnergyMeasurementRepository measurementRepository)
    {
        _measurementRepository = measurementRepository;
    }

    /// <summary>
    /// Handles the GetEnergyMeasurementsQuery
    /// Retrieves energy measurements for a device, optionally filtered by measurement type
    /// </summary>
    /// <param name="request">Query request with device ID, time range, and optional measurement type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of energy measurements</returns>
    public async Task<IEnumerable<EnergyMeasurement>> Handle(
        GetEnergyMeasurementsQuery request, 
        CancellationToken cancellationToken)
    {
        // If measurement type is specified, filter by type
        if (request.MeasurementType.HasValue)
        {
            return await _measurementRepository.GetMeasurementsByTypeAsync(
                request.DeviceId, 
                request.MeasurementType.Value, 
                request.Range, 
                cancellationToken);
        }

        // Otherwise, get all measurements for the device
        return await _measurementRepository.GetMeasurementsAsync(
            request.DeviceId, 
            request.Range, 
            cancellationToken);
    }

    /// <summary>
    /// Handles the GetAggregatedEnergyDataQuery
    /// Retrieves aggregated energy data using TimescaleDB time_bucket function
    /// </summary>
    /// <param name="request">Query request with device ID, time range, and aggregation interval</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of aggregated energy measurements</returns>
    public async Task<IEnumerable<EnergyMeasurement>> Handle(
        GetAggregatedEnergyDataQuery request, 
        CancellationToken cancellationToken)
    {
        return await _measurementRepository.GetAggregatedDataAsync(
            request.DeviceId, 
            request.Interval, 
            request.Range, 
            cancellationToken);
    }

    /// <summary>
    /// Handles the GetSiteEnergyMeasurementsQuery
    /// Retrieves all energy measurements for a site within a time range
    /// </summary>
    /// <param name="request">Query request with site ID and time range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of energy measurements for the site</returns>
    public async Task<IEnumerable<EnergyMeasurement>> Handle(
        GetSiteEnergyMeasurementsQuery request, 
        CancellationToken cancellationToken)
    {
        return await _measurementRepository.GetSiteMeasurementsAsync(
            request.SiteId, 
            request.Range, 
            cancellationToken);
    }

    /// <summary>
    /// Handles the GetLatestMeasurementQuery
    /// Retrieves the most recent measurement for a device, optionally filtered by type
    /// </summary>
    /// <param name="request">Query request with device ID and optional measurement type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest energy measurement or null if none found</returns>
    public async Task<EnergyMeasurement?> Handle(
        GetLatestMeasurementQuery request, 
        CancellationToken cancellationToken)
    {
        return await _measurementRepository.GetLatestMeasurementAsync(
            request.DeviceId, 
            request.MeasurementType, 
            cancellationToken);
    }

    /// <summary>
    /// Handles the GetMeasurementStatisticsQuery
    /// Calculates statistics for measurements of a specific type within a time range
    /// </summary>
    /// <param name="request">Query request with device ID, measurement type, and time range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Measurement statistics including average, min, max, count, etc.</returns>
    public async Task<IMeasurementStatistics> Handle(
        GetMeasurementStatisticsQuery request, 
        CancellationToken cancellationToken)
    {
        return await _measurementRepository.GetMeasurementStatisticsAsync(
            request.DeviceId, 
            request.MeasurementType, 
            request.Range, 
            cancellationToken);
    }
}
