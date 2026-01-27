using MediatR;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Interfaces;
using EMSCore.Domain.Enums;
using System;

namespace EMSCore.Application.Queries;

/// <summary>
/// Query to retrieve energy measurements for a specific device within a time range
/// </summary>
/// <param name="DeviceId">Device identifier to get measurements for</param>
/// <param name="Range">Time range for the measurements</param>
/// <param name="MeasurementType">Optional filter by measurement type</param>
public record GetEnergyMeasurementsQuery(
    string DeviceId,
    DateTimeRange Range,
    MeasurementType? MeasurementType = null
) : IRequest<IEnumerable<EnergyMeasurement>>;

/// <summary>
/// Query to retrieve aggregated energy measurements using TimescaleDB time_bucket
/// </summary>
/// <param name="DeviceId">Device identifier to get measurements for</param>
/// <param name="Range">Time range for the measurements</param>
/// <param name="Interval">Aggregation interval (e.g., 1 hour, 15 minutes)</param>
public record GetAggregatedEnergyDataQuery(
    string DeviceId,
    DateTimeRange Range,
    TimeSpan Interval
) : IRequest<IEnumerable<EnergyMeasurement>>;

/// <summary>
/// Query to retrieve energy measurements for an entire site
/// </summary>
/// <param name="SiteId">Site identifier to get measurements for</param>
/// <param name="Range">Time range for the measurements</param>
public record GetSiteEnergyMeasurementsQuery(
    string SiteId,
    DateTimeRange Range
) : IRequest<IEnumerable<EnergyMeasurement>>;

/// <summary>
/// Query to get the latest measurement for a device
/// </summary>
/// <param name="DeviceId">Device identifier</param>
/// <param name="MeasurementType">Optional filter by measurement type</param>
public record GetLatestMeasurementQuery(
    string DeviceId,
    MeasurementType? MeasurementType = null
) : IRequest<EnergyMeasurement?>;

/// <summary>
/// Query to get measurement statistics for a device and time range
/// </summary>
/// <param name="DeviceId">Device identifier</param>
/// <param name="MeasurementType">Type of measurement to analyze</param>
/// <param name="Range">Time range for statistics</param>
public record GetMeasurementStatisticsQuery(
    string DeviceId,
    MeasurementType MeasurementType,
    DateTimeRange Range
) : IRequest<IMeasurementStatistics>;
