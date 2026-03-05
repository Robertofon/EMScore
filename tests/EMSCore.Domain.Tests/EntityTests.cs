using Xunit;
using EMSCore.Domain.Entities;
using EMSCore.Domain.Enums;

namespace EMSCore.Domain.Tests.Entities;

public class SiteTests
{
    [Fact]
    public void Site_WithValidData_ShouldCreate()
    {
        var site = new Site
        {
            Id = "site-001",
            Name = "Test Site",
            Location = "Test Location",
            IsActive = true
        };

        Assert.Equal("site-001", site.Id);
        Assert.Equal("Test Site", site.Name);
        Assert.Equal("Test Location", site.Location);
        Assert.True(site.IsActive);
    }

    [Fact]
    public void Site_DefaultValues_ShouldBeActive()
    {
        var site = new Site
        {
            Id = "site-001",
            Name = "Test Site",
            Location = "Test Location"
        };

        // IsActive defaults to true in the entity
        Assert.True(site.IsActive);
    }
}

public class DeviceTests
{
    [Fact]
    public void Device_WithValidData_ShouldCreate()
    {
        var device = new Device
        {
            Id = "device-001",
            Name = "Solar Panel 1",
            Type = DeviceType.SolarPanel,
            SiteId = "site-001",
            IsActive = true,
            IsOnline = false
        };

        Assert.Equal("device-001", device.Id);
        Assert.Equal("Solar Panel 1", device.Name);
        Assert.Equal(DeviceType.SolarPanel, device.Type);
        Assert.Equal("site-001", device.SiteId);
        Assert.True(device.IsActive);
        Assert.False(device.IsOnline);
    }

    [Fact]
    public void Device_DefaultValues_ShouldBeActive()
    {
        var device = new Device
        {
            Id = "device-001",
            Name = "Test Device",
            Type = DeviceType.Battery,
            SiteId = "site-001"
        };

        // IsActive defaults to true in the entity
        Assert.True(device.IsActive);
        // IsOnline defaults to false
        Assert.False(device.IsOnline);
    }
}

public class EnergyMeasurementTests
{
    [Fact]
    public void EnergyMeasurement_WithValidData_ShouldCreate()
    {
        var measurement = new EnergyMeasurement
        {
            Id = 1,
            DeviceId = "device-001",
            SiteId = "site-001",
            Type = MeasurementType.Power,
            Value = 1500.5,
            Unit = "W",
            Timestamp = DateTime.UtcNow,
            Quality = QualityFlag.Good
        };

        Assert.Equal(1, measurement.Id);
        Assert.Equal("device-001", measurement.DeviceId);
        Assert.Equal(MeasurementType.Power, measurement.Type);
        Assert.Equal(1500.5, measurement.Value);
        Assert.Equal("W", measurement.Unit);
        Assert.Equal(QualityFlag.Good, measurement.Quality);
    }

    [Fact]
    public void EnergyMeasurement_DefaultQuality_ShouldBeGood()
    {
        var measurement = new EnergyMeasurement
        {
            DeviceId = "device-001",
            SiteId = "site-001",
            Type = MeasurementType.Voltage,
            Value = 230.0,
            Unit = "V",
            Timestamp = DateTime.UtcNow
        };

        Assert.Equal(QualityFlag.Good, measurement.Quality);
    }
}
