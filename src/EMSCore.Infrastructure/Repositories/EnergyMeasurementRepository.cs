using Microsoft.EntityFrameworkCore;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Enums;
using EMSCore.Domain.Interfaces;
using EMSCore.Infrastructure.Data;
using System;

namespace EMSCore.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for energy measurement operations using EF Core and TimescaleDB
/// </summary>
public class EnergyMeasurementRepository : IEnergyMeasurementRepository
{
    private readonly EMSDbContext _context;

    public EnergyMeasurementRepository(EMSDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EnergyMeasurement>> GetMeasurementsAsync(
        string deviceId,
        DateTimeRange range,
        CancellationToken cancellationToken = default)
    {
        return await _context.EnergyMeasurements
            .Where(m => m.DeviceId == deviceId &&
                       m.Timestamp >= range.Start &&
                       m.Timestamp <= range.End)
            .OrderBy(m => m.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EnergyMeasurement>> GetSiteMeasurementsAsync(
        string siteId,
        DateTimeRange range,
        CancellationToken cancellationToken = default)
    {
        return await _context.EnergyMeasurements
            .Where(m => m.SiteId == siteId &&
                       m.Timestamp >= range.Start &&
                       m.Timestamp <= range.End)
            .OrderBy(m => m.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EnergyMeasurement>> GetMeasurementsByTypeAsync(
        string deviceId,
        MeasurementType measurementType,
        DateTimeRange range,
        CancellationToken cancellationToken = default)
    {
        return await _context.EnergyMeasurements
            .Where(m => m.DeviceId == deviceId &&
                       m.Type == measurementType &&
                       m.Timestamp >= range.Start &&
                       m.Timestamp <= range.End)
            .OrderBy(m => m.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EnergyMeasurement>> GetAggregatedDataAsync(
        string deviceId,
        TimeSpan interval,
        DateTimeRange range,
        CancellationToken cancellationToken = default)
    {
        // Use TimescaleDB time_bucket function for efficient aggregation
        var intervalString = FormatInterval(interval);
        
        var sql = @"
            SELECT
                time_bucket(@interval, timestamp) as timestamp,
                device_id,
                site_id,
                measurement_type,
                unit,
                AVG(value) as value,
                0 as quality_flag,
                'aggregated' as aggregation_level,
                COUNT(*)::int as sample_count,
                MIN(value) as min_value,
                MAX(value) as max_value,
                NOW() as created_at,
                0 as id,
                NULL as metadata,
                NULL as phase
            FROM energy_measurements
            WHERE device_id = @deviceId
                AND timestamp >= @startTime
                AND timestamp <= @endTime
                AND aggregation_level = 'raw'
            GROUP BY time_bucket(@interval, timestamp), device_id, site_id, measurement_type, unit
            ORDER BY timestamp";

        var parameters = new[]
        {
            new Npgsql.NpgsqlParameter("@interval", intervalString),
            new Npgsql.NpgsqlParameter("@deviceId", deviceId),
            new Npgsql.NpgsqlParameter("@startTime", range.Start),
            new Npgsql.NpgsqlParameter("@endTime", range.End)
        };

        return await _context.EnergyMeasurements
            .FromSqlRaw(sql, parameters)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<EnergyMeasurement> AddMeasurementAsync(
        EnergyMeasurement measurement, 
        CancellationToken cancellationToken = default)
    {
        measurement.CreatedAt = DateTime.UtcNow;
        
        _context.EnergyMeasurements.Add(measurement);
        await _context.SaveChangesAsync(cancellationToken);
        
        return measurement;
    }

    public async Task<int> AddMeasurementsBatchAsync(
        IEnumerable<EnergyMeasurement> measurements, 
        CancellationToken cancellationToken = default)
    {
        var measurementList = measurements.ToList();
        var now = DateTime.UtcNow;
        
        foreach (var measurement in measurementList)
        {
            measurement.CreatedAt = now;
        }
        
        _context.EnergyMeasurements.AddRange(measurementList);
        await _context.SaveChangesAsync(cancellationToken);
        
        return measurementList.Count;
    }

    public async Task<EnergyMeasurement?> GetLatestMeasurementAsync(
        string deviceId, 
        MeasurementType? measurementType = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.EnergyMeasurements
            .Where(m => m.DeviceId == deviceId);

        if (measurementType.HasValue)
        {
            query = query.Where(m => m.Type == measurementType.Value);
        }

        return await query
            .OrderByDescending(m => m.Timestamp)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IMeasurementStatistics> GetMeasurementStatisticsAsync(
        string deviceId,
        MeasurementType measurementType,
        DateTimeRange range,
        CancellationToken cancellationToken = default)
    {
        var measurements = await _context.EnergyMeasurements
            .Where(m => m.DeviceId == deviceId &&
                       m.Type == measurementType &&
                       m.Timestamp >= range.Start &&
                       m.Timestamp <= range.End)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!measurements.Any())
        {
            return new MeasurementStatistics(0, 0, 0, 0, 0, DateTime.MinValue, DateTime.MinValue);
        }

        var values = measurements.Select(m => m.Value).ToList();
        var timestamps = measurements.Select(m => m.Timestamp).ToList();

        return new MeasurementStatistics(
            Average: values.Average(),
            Minimum: values.Min(),
            Maximum: values.Max(),
            Sum: values.Sum(),
            Count: values.Count,
            FirstTimestamp: timestamps.Min(),
            LastTimestamp: timestamps.Max()
        );
    }

    /// <summary>
    /// Formats TimeSpan to PostgreSQL interval format
    /// </summary>
    private static string FormatInterval(TimeSpan interval)
    {
        if (interval.TotalDays >= 1)
        {
            return $"{(int)interval.TotalDays} days";
        }
        if (interval.TotalHours >= 1)
        {
            return $"{(int)interval.TotalHours} hours";
        }
        if (interval.TotalMinutes >= 1)
        {
            return $"{(int)interval.TotalMinutes} minutes";
        }
        
        return $"{(int)interval.TotalSeconds} seconds";
    }
}
