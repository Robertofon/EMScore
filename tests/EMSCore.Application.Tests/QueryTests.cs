using Xunit;
using EMSCore.Application.Queries;
using EMSCore.Application.Commands;
using EMSCore.Domain.Enums;

namespace EMSCore.Application.Tests.Queries;

public class GetEnergyMeasurementsQueryTests
{
    [Fact]
    public void GetEnergyMeasurementsQuery_WithValidParams_ShouldCreate()
    {
        var range = new DateTimeRange(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
        var query = new GetEnergyMeasurementsQuery("device-001", range);

        Assert.Equal("device-001", query.DeviceId);
        Assert.Equal(range, query.Range);
        Assert.Null(query.MeasurementType);
    }

    [Fact]
    public void GetEnergyMeasurementsQuery_WithMeasurementType_ShouldCreate()
    {
        var range = new DateTimeRange(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
        var query = new GetEnergyMeasurementsQuery("device-001", range, MeasurementType.Power);

        Assert.Equal("device-001", query.DeviceId);
        Assert.Equal(MeasurementType.Power, query.MeasurementType);
    }
}

public class GetAggregatedEnergyDataQueryTests
{
    [Fact]
    public void GetAggregatedEnergyDataQuery_WithValidParams_ShouldCreate()
    {
        var range = new DateTimeRange(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
        var interval = TimeSpan.FromHours(1);
        var query = new GetAggregatedEnergyDataQuery("device-001", range, interval);

        Assert.Equal("device-001", query.DeviceId);
        Assert.Equal(interval, query.Interval);
        Assert.Equal(range, query.Range);
    }
}

public class GetLatestMeasurementQueryTests
{
    [Fact]
    public void GetLatestMeasurementQuery_WithoutMeasurementType_ShouldCreate()
    {
        var query = new GetLatestMeasurementQuery("device-001");

        Assert.Equal("device-001", query.DeviceId);
        Assert.Null(query.MeasurementType);
    }

    [Fact]
    public void GetLatestMeasurementQuery_WithMeasurementType_ShouldCreate()
    {
        var query = new GetLatestMeasurementQuery("device-001", MeasurementType.Voltage);

        Assert.Equal("device-001", query.DeviceId);
        Assert.Equal(MeasurementType.Voltage, query.MeasurementType);
    }
}

public class GetMeasurementStatisticsQueryTests
{
    [Fact]
    public void GetMeasurementStatisticsQuery_WithValidParams_ShouldCreate()
    {
        var range = new DateTimeRange(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
        var query = new GetMeasurementStatisticsQuery("device-001", MeasurementType.Power, range);

        Assert.Equal("device-001", query.DeviceId);
        Assert.Equal(MeasurementType.Power, query.MeasurementType);
        Assert.Equal(range, query.Range);
    }
}
