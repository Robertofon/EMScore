using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using EMSCore.Domain.Interfaces;
using EMSCore.Infrastructure.Drivers;

namespace EMSCore.Tests.Drivers;

/// <summary>
/// Unit tests for ShellyActuator driver implementation
/// </summary>
public class ShellyActuatorTests
{
    private readonly Mock<ILogger<ShellyActuator>> _loggerMock;
    private readonly Mock<IMqttService> _mqttServiceMock;

    public ShellyActuatorTests()
    {
        _loggerMock = new Mock<ILogger<ShellyActuator>>();
        _mqttServiceMock = new Mock<IMqttService>();
    }

    /// <summary>
    /// Tests that ConnectAsync returns true and sets connected state with valid configuration
    /// </summary>
    [Fact]
    public async Task ConnectAsync_WithValidConfig_ShouldReturnTrue()
    {
        var actuator = new ShellyActuator(_loggerMock.Object, _mqttServiceMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "DeviceId", "shelly-plug-001" },
                { "RelayIndex", "0" }
            }
        };

        var result = await actuator.ConnectAsync(config);

        Assert.True(result);
        Assert.True(actuator.IsConnected);
        Assert.Equal(DriverState.Connected, actuator.State);
    }

    /// <summary>
    /// Tests that DisconnectAsync properly disconnects and resets state
    /// </summary>
    [Fact]
    public async Task DisconnectAsync_WhenConnected_ShouldSetStateToDisconnected()
    {
        var actuator = new ShellyActuator(_loggerMock.Object, _mqttServiceMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "DeviceId", "shelly-plug-001" }
            }
        };

        await actuator.ConnectAsync(config);
        await actuator.DisconnectAsync();

        Assert.False(actuator.IsConnected);
        Assert.Equal(DriverState.Disconnected, actuator.State);
    }

    /// <summary>
    /// Tests that WriteRegistersAsync publishes MQTT message with integer value 1
    /// </summary>
    [Fact]
    public async Task WriteRegistersAsync_WithOnValue_ShouldPublishMqttMessage()
    {
        var actuator = new ShellyActuator(_loggerMock.Object, _mqttServiceMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "DeviceId", "shelly-plug-001" },
                { "RelayIndex", "0" }
            }
        };

        await actuator.ConnectAsync(config);

        _mqttServiceMock
            .Setup(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var registers = new[]
        {
            new DriverRegister { Address = "0", Value = 1, Type = RegisterType.HoldingRegister }
        };

        await actuator.WriteRegistersAsync(registers);

        _mqttServiceMock.Verify(
            x => x.PublishAsync(
                "shellies/shelly-plug-001/relay/0/command",
                It.IsAny<object>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that WriteRegistersAsync publishes MQTT message with integer value 0
    /// </summary>
    [Fact]
    public async Task WriteRegistersAsync_WithOffValue_ShouldPublishMqttMessage()
    {
        var actuator = new ShellyActuator(_loggerMock.Object, _mqttServiceMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "DeviceId", "shelly-plug-001" },
                { "RelayIndex", "0" }
            }
        };

        await actuator.ConnectAsync(config);

        _mqttServiceMock
            .Setup(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var registers = new[]
        {
            new DriverRegister { Address = "0", Value = 0, Type = RegisterType.HoldingRegister }
        };

        await actuator.WriteRegistersAsync(registers);

        _mqttServiceMock.Verify(
            x => x.PublishAsync(
                "shellies/shelly-plug-001/relay/0/command",
                It.IsAny<object>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that WriteRegistersAsync correctly handles boolean values
    /// </summary>
    [Fact]
    public async Task WriteRegistersAsync_WithBoolValue_ShouldWork()
    {
        var actuator = new ShellyActuator(_loggerMock.Object, _mqttServiceMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "DeviceId", "shelly-plug-001" }
            }
        };

        await actuator.ConnectAsync(config);

        _mqttServiceMock
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var registers = new[]
        {
            new DriverRegister { Address = "0", Value = true, Type = RegisterType.HoldingRegister }
        };

        await actuator.WriteRegistersAsync(registers);

        _mqttServiceMock.Verify(
            x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that WriteRegistersAsync does nothing when not connected
    /// </summary>
    [Fact]
    public async Task WriteRegistersAsync_WhenNotConnected_ShouldNotPublish()
    {
        var actuator = new ShellyActuator(_loggerMock.Object, _mqttServiceMock.Object);

        var registers = new[]
        {
            new DriverRegister { Address = "0", Value = 1, Type = RegisterType.HoldingRegister }
        };

        await actuator.WriteRegistersAsync(registers);

        _mqttServiceMock.Verify(
            x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Tests that ReadRegistersAsync returns empty when not connected
    /// </summary>
    [Fact]
    public async Task ReadRegistersAsync_WhenNotConnected_ShouldReturnEmpty()
    {
        var actuator = new ShellyActuator(_loggerMock.Object, _mqttServiceMock.Object);
        
        var registers = await actuator.ReadRegistersAsync(new[] { "0" });

        Assert.NotNull(registers);
        Assert.Empty(registers);
    }

    /// <summary>
    /// Tests that SetRelayStateAsync publishes MQTT message when turning on
    /// </summary>
    [Fact]
    public async Task SetRelayStateAsync_WithTrue_ShouldPublishOnCommand()
    {
        var actuator = new ShellyActuator(_loggerMock.Object, _mqttServiceMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "DeviceId", "shelly-plug-001" },
                { "RelayIndex", "0" }
            }
        };

        await actuator.ConnectAsync(config);

        _mqttServiceMock
            .Setup(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await actuator.SetRelayStateAsync(true);

        _mqttServiceMock.Verify(
            x => x.PublishAsync(
                "shellies/shelly-plug-001/relay/0/command",
                It.IsAny<object>(),
                1,
                false,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that SetRelayStateAsync publishes MQTT message when turning off
    /// </summary>
    [Fact]
    public async Task SetRelayStateAsync_WithFalse_ShouldPublishOffCommand()
    {
        var actuator = new ShellyActuator(_loggerMock.Object, _mqttServiceMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "DeviceId", "shelly-plug-001" },
                { "RelayIndex", "0" }
            }
        };

        await actuator.ConnectAsync(config);

        _mqttServiceMock
            .Setup(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await actuator.SetRelayStateAsync(false);

        _mqttServiceMock.Verify(
            x => x.PublishAsync(
                "shellies/shelly-plug-001/relay/0/command",
                It.IsAny<object>(),
                1,
                false,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that ToggleRelayAsync toggles from off to on
    /// </summary>
    [Fact]
    public async Task ToggleRelayAsync_WhenOff_ShouldTurnOn()
    {
        var actuator = new ShellyActuator(_loggerMock.Object, _mqttServiceMock.Object);
        var config = new DriverConfiguration
        {
            Parameters = new Dictionary<string, string>
            {
                { "DeviceId", "shelly-plug-001" }
            }
        };

        await actuator.ConnectAsync(config);

        _mqttServiceMock
            .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await actuator.ToggleRelayAsync();

        _mqttServiceMock.Verify(
            x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
