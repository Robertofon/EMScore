using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using EMSCore.Domain.Interfaces;
using EMSCore.Infrastructure.Drivers;

namespace EMSCore.Tests.Drivers;

public class ModbusRtuDriverTests
{
    private readonly Mock<ILogger<ModbusRtuDriver>> _loggerMock;

    public ModbusRtuDriverTests()
    {
        _loggerMock = new Mock<ILogger<ModbusRtuDriver>>();
    }

    [Fact]
    public async Task ConnectAsync_WithValidConfig_ShouldReturnTrue()
    {
        var driver = new ModbusRtuDriver(_loggerMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "Port", "COM1" },
                { "SlaveAddress", "1" },
                { "BaudRate", "9600" }
            }
        };

        var result = await driver.ConnectAsync(config);

        Assert.True(result);
        Assert.True(driver.IsConnected);
        Assert.Equal(DriverState.Connected, driver.State);
    }

    [Fact]
    public async Task ConnectAsync_WithInvalidPort_ShouldReturnTrue_InSimulationMode()
    {
        var driver = new ModbusRtuDriver(_loggerMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "Port", "INVALID_PORT" }
            }
        };

        var result = await driver.ConnectAsync(config);

        // In simulation mode, connection always succeeds
        Assert.True(result);
    }

    [Fact]
    public async Task DisconnectAsync_WhenConnected_ShouldSetStateToDisconnected()
    {
        var driver = new ModbusRtuDriver(_loggerMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "Port", "COM1" },
                { "SlaveAddress", "1" }
            }
        };

        await driver.ConnectAsync(config);
        await driver.DisconnectAsync();

        Assert.False(driver.IsConnected);
        Assert.Equal(DriverState.Disconnected, driver.State);
    }

    [Fact]
    public async Task ReadRegistersAsync_WhenConnected_ShouldReturnRegisters()
    {
        var driver = new ModbusRtuDriver(_loggerMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "Port", "COM1" },
                { "SlaveAddress", "1" }
            }
        };

        await driver.ConnectAsync(config);
        var registers = await driver.ReadRegistersAsync(new[] { "0", "1", "2" });

        Assert.NotNull(registers);
        Assert.Equal(3, registers.Count());
        foreach (var reg in registers)
        {
            Assert.Equal(EMSCore.Domain.Enums.QualityFlag.Good, reg.Quality);
        }
    }

    [Fact]
    public async Task ReadRegistersAsync_WhenNotConnected_ShouldReturnEmpty()
    {
        var driver = new ModbusRtuDriver(_loggerMock.Object);
        
        var registers = await driver.ReadRegistersAsync(new[] { "0" });

        Assert.NotNull(registers);
        Assert.Empty(registers);
    }

    [Fact]
    public async Task WriteRegistersAsync_WhenConnected_ShouldNotThrow()
    {
        var driver = new ModbusRtuDriver(_loggerMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "Port", "COM1" },
                { "SlaveAddress", "1" }
            }
        };

        await driver.ConnectAsync(config);
        
        var registers = new[]
        {
            new DriverRegister { Address = "0", Value = (ushort)100, Type = RegisterType.HoldingRegister }
        };

        await driver.WriteRegistersAsync(registers);
    }

    [Fact]
    public async Task DiscoverRegistersAsync_WhenConnected_ShouldReturnRegisterDefinitions()
    {
        var driver = new ModbusRtuDriver(_loggerMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "Port", "COM1" },
                { "SlaveAddress", "1" }
            }
        };

        await driver.ConnectAsync(config);
        var registers = await driver.DiscoverRegistersAsync();

        Assert.NotNull(registers);
        Assert.NotEmpty(registers);
    }

    [Fact]
    public async Task ValidateConnectionAsync_WhenConnected_ShouldReturnTrue()
    {
        var driver = new ModbusRtuDriver(_loggerMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "Port", "COM1" },
                { "SlaveAddress", "1" }
            }
        };

        await driver.ConnectAsync(config);
        var isValid = await driver.ValidateConnectionAsync();

        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateConnectionAsync_WhenNotConnected_ShouldReturnFalse()
    {
        var driver = new ModbusRtuDriver(_loggerMock.Object);
        
        var isValid = await driver.ValidateConnectionAsync();

        Assert.False(isValid);
    }
}

public class ModbusTcpDriverTests
{
    private readonly Mock<ILogger<ModbusTcpDriver>> _loggerMock;

    public ModbusTcpDriverTests()
    {
        _loggerMock = new Mock<ILogger<ModbusTcpDriver>>();
    }

    [Fact]
    public async Task ConnectAsync_WithValidConfig_ShouldReturnTrue()
    {
        var driver = new ModbusTcpDriver(_loggerMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "Host", "localhost" },
                { "Port", "502" },
                { "SlaveAddress", "1" }
            }
        };

        var result = await driver.ConnectAsync(config);

        Assert.True(result);
        Assert.True(driver.IsConnected);
        Assert.Equal(DriverState.Connected, driver.State);
    }

    [Fact]
    public async Task ConnectAsync_WithInvalidHost_ShouldReturnTrue_InSimulationMode()
    {
        var driver = new ModbusTcpDriver(_loggerMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "Host", "invalid.host.that.does.not.exist" },
                { "Port", "99999" }
            }
        };

        var result = await driver.ConnectAsync(config);

        // In simulation mode, connection always succeeds
        Assert.True(result);
    }

    [Fact]
    public async Task DisconnectAsync_WhenConnected_ShouldSetStateToDisconnected()
    {
        var driver = new ModbusTcpDriver(_loggerMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "Host", "localhost" },
                { "Port", "502" },
                { "SlaveAddress", "1" }
            }
        };

        await driver.ConnectAsync(config);
        await driver.DisconnectAsync();

        Assert.False(driver.IsConnected);
        Assert.Equal(DriverState.Disconnected, driver.State);
    }

    [Fact]
    public async Task ReadRegistersAsync_WhenConnected_ShouldReturnRegisters()
    {
        var driver = new ModbusTcpDriver(_loggerMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "Host", "localhost" },
                { "Port", "502" },
                { "SlaveAddress", "1" }
            }
        };

        await driver.ConnectAsync(config);
        var registers = await driver.ReadRegistersAsync(new[] { "0", "1", "2" });

        Assert.NotNull(registers);
        Assert.Equal(3, registers.Count());
    }

    [Fact]
    public async Task DiscoverRegistersAsync_WhenConnected_ShouldReturnRegisterDefinitions()
    {
        var driver = new ModbusTcpDriver(_loggerMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "Host", "localhost" },
                { "Port", "502" },
                { "SlaveAddress", "1" }
            }
        };

        await driver.ConnectAsync(config);
        var registers = await driver.DiscoverRegistersAsync();

        Assert.NotNull(registers);
        Assert.NotEmpty(registers);
    }

    [Fact]
    public async Task ValidateConnectionAsync_WhenConnected_ShouldReturnTrue()
    {
        var driver = new ModbusTcpDriver(_loggerMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "Host", "localhost" },
                { "Port", "502" },
                { "SlaveAddress", "1" }
            }
        };

        await driver.ConnectAsync(config);
        var isValid = await driver.ValidateConnectionAsync();

        Assert.True(isValid);
    }
}
