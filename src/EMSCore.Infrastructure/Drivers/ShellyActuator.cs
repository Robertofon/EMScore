using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using EMSCore.Domain.Interfaces;
using EMSCore.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EMSCore.Infrastructure.Drivers;

/// <summary>
/// IDriver implementation for Shelly smart plugs that enables relay control via MQTT or HTTP.
/// Supports turning the relay on/off, reading current state, and toggling.
/// </summary>
/// <remarks>
/// This actuator can operate in two modes:
/// - MQTT mode (default): Publishes commands to the Shelly device via MQTT broker
/// - HTTP mode: Sends HTTP requests directly to the Shelly device REST API
/// </remarks>
public class ShellyActuator : IDriver
{
    private readonly ILogger<ShellyActuator> _logger;
    private readonly IMqttService _mqttService;
    private readonly HttpClient _httpClient;
    private DriverConfiguration _config = new();
    private CancellationTokenSource? _eventCts;
    private readonly Channel<DriverEvent> _eventChannel;
    private ActuatorState _currentState = ActuatorState.Unknown;

    /// <inheritdoc />
    public string Name => "Shelly Actuator";

    /// <inheritdoc />
    public string Protocol => "Shelly MQTT/HTTP";

    /// <inheritdoc />
    public DriverState State { get; private set; } = DriverState.Disconnected;

    /// <inheritdoc />
    public bool IsConnected => State == DriverState.Connected;

    /// <summary>
    /// Creates a new ShellyActuator instance
    /// </summary>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <param name="mqttService">MQTT service for publishing commands</param>
    /// <param name="httpClient">Optional HTTP client for HTTP mode</param>
    public ShellyActuator(
        ILogger<ShellyActuator> logger,
        IMqttService mqttService,
        HttpClient? httpClient = null)
    {
        _logger = logger;
        _mqttService = mqttService;
        _httpClient = httpClient ?? new HttpClient();
        _eventChannel = Channel.CreateUnbounded<DriverEvent>();
    }

    /// <inheritdoc />
    public Task<bool> ConnectAsync(DriverConfiguration config, CancellationToken ct = default)
    {
        try
        {
            State = DriverState.Connecting;
            _config = config;

            var deviceId = config.Parameters.GetValueOrDefault("DeviceId", "shelly-plug");
            var relayIndex = int.Parse(config.Parameters.GetValueOrDefault("RelayIndex", "0"));
            var timeout = int.Parse(config.Parameters.GetValueOrDefault("TimeoutMs", "5000"));

            _httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);

            State = DriverState.Connected;
            
            _eventCts = new CancellationTokenSource();
            _ = EventStreamWorker(_eventCts.Token);
            
            _logger.LogInformation("Shelly Actuator connected for device {DeviceId}, relay {RelayIndex}", deviceId, relayIndex);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect Shelly Actuator");
            State = DriverState.Error;
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _eventCts?.Cancel();
        State = DriverState.Disconnected;
        _currentState = ActuatorState.Unknown;
        _logger.LogInformation("Shelly Actuator disconnected");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reads the current relay state. When UseHttp=true in config, queries the device via HTTP REST API.
    /// Otherwise returns the locally tracked state.
    /// </remarks>
    public async Task<IEnumerable<DriverRegister>> ReadRegistersAsync(IEnumerable<string> addresses, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Not connected to Shelly device");
            return Enumerable.Empty<DriverRegister>();
        }

        var results = new List<DriverRegister>();
        var addressList = addresses.ToList();

        foreach (var address in addressList)
        {
            try
            {
                var register = await ReadRegisterAsync(address, ct);
                results.Add(register);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read register {Address}", address);
                results.Add(new DriverRegister
                {
                    Address = address,
                    Type = RegisterType.HoldingRegister,
                    Quality = QualityFlag.Bad,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        return results;
    }

    private async Task<DriverRegister> ReadRegisterAsync(string address, CancellationToken ct)
    {
        var deviceId = _config.Parameters.GetValueOrDefault("DeviceId", "shelly-plug");
        var relayIndex = int.Parse(_config.Parameters.GetValueOrDefault("RelayIndex", "0"));
        var useHttp = _config.Parameters.GetValueOrDefault("UseHttp", "false").Equals("true", StringComparison.OrdinalIgnoreCase);

        if (useHttp)
        {
            var ipAddress = _config.Parameters.GetValueOrDefault("IpAddress", "192.168.1.1");
            var url = $"http://{ipAddress}/relay/{relayIndex}";
            
            var response = await _httpClient.GetFromJsonAsync<ShellyRelayStatusResponse>(url, ct);
            
            var isOn = response?.Output?.On ?? false;
            _currentState = isOn ? ActuatorState.On : ActuatorState.Off;

            return new DriverRegister
            {
                Address = address,
                Type = RegisterType.HoldingRegister,
                Value = isOn ? 1 : 0,
                Quality = QualityFlag.Good,
                Timestamp = DateTime.UtcNow
            };
        }
        else
        {
            return new DriverRegister
            {
                Address = address,
                Type = RegisterType.HoldingRegister,
                Value = _currentState == ActuatorState.On ? 1 : 0,
                Quality = QualityFlag.Good,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Writes to registers to control the relay. Supports bool, int, and double values.
    /// Non-zero values turn the relay on, zero values turn it off.
    /// </remarks>
    public async Task WriteRegistersAsync(IEnumerable<DriverRegister> registers, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Not connected to Shelly device");
            return;
        }

        foreach (var register in registers)
        {
            try
            {
                var value = register.Value;
                bool turnOn;

                if (value is bool boolValue)
                {
                    turnOn = boolValue;
                }
                else if (value is int intValue)
                {
                    turnOn = intValue != 0;
                }
                else if (value is double doubleValue)
                {
                    turnOn = doubleValue != 0;
                }
                else
                {
                    _logger.LogWarning("Unsupported register value type: {Type}", value?.GetType().Name);
                    continue;
                }

                await SetRelayStateAsync(turnOn, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write register {Address}", register.Address);
            }
        }
    }

    /// <summary>
    /// Sets the relay to a specific state (on or off)
    /// </summary>
    /// <param name="turnOn">True to turn relay on, false to turn off</param>
    /// <param name="ct">Cancellation token</param>
    /// <remarks>
    /// Uses MQTT by default, or HTTP if UseHttp=true in configuration.
    /// MQTT topic: shellies/{deviceId}/relay/{index}/command with payload {"turn":"on"} or {"turn":"off"}
    /// </remarks>
    public async Task SetRelayStateAsync(bool turnOn, CancellationToken ct = default)
    {
        var deviceId = _config.Parameters.GetValueOrDefault("DeviceId", "shelly-plug");
        var relayIndex = int.Parse(_config.Parameters.GetValueOrDefault("RelayIndex", "0"));
        var useHttp = _config.Parameters.GetValueOrDefault("UseHttp", "false").Equals("true", StringComparison.OrdinalIgnoreCase);

        if (useHttp)
        {
            var ipAddress = _config.Parameters.GetValueOrDefault("IpAddress", "192.168.1.1");
            var url = $"http://{ipAddress}/relay/{relayIndex}/command?turn={(turnOn ? "on" : "off")}";
            
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            _currentState = turnOn ? ActuatorState.On : ActuatorState.Off;
            _logger.LogInformation("Shelly relay set to {State} via HTTP", turnOn ? "on" : "off");
        }
        else
        {
            var topic = $"shellies/{deviceId}/relay/{relayIndex}/command";
            var payload = new ShellyCommandPayload { Turn = turnOn ? "on" : "off" };
            
            await _mqttService.PublishAsync(topic, payload, cancellationToken: ct);
            
            _currentState = turnOn ? ActuatorState.On : ActuatorState.Off;
            _logger.LogInformation("Shelly relay set to {State} via MQTT", turnOn ? "on" : "off");
        }

        await _eventChannel.Writer.WriteAsync(new DriverEvent
        {
            EventType = "RelayStateChanged",
            Timestamp = DateTime.UtcNow,
            Message = $"Relay turned {(turnOn ? "on" : "off")}",
            Data = new Dictionary<string, object>
            {
                { "state", turnOn ? "on" : "off" },
                { "timestamp", DateTime.UtcNow }
            }
        }, ct);
    }

    /// <summary>
    /// Toggles the current relay state
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public async Task ToggleRelayAsync(CancellationToken ct = default)
    {
        var newState = _currentState != ActuatorState.On;
        await SetRelayStateAsync(newState, ct);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<DriverEvent> EventStream(CancellationToken ct = default)
    {
        return _eventChannel.Reader.ReadAllAsync(ct);
    }

    private async Task EventStreamWorker(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Shelly event stream worker");
        }
    }
}

/// <summary>
/// Response from Shelly device REST API for relay status
/// </summary>
public class ShellyRelayStatusResponse
{
    /// <summary>Whether the relay is currently on</summary>
    [JsonPropertyName("ison")]
    public bool? IsOn { get; set; }
    
    /// <summary>Relay output details</summary>
    [JsonPropertyName("output")]
    public ShellyRelayOutput? Output { get; set; }
    
    /// <summary>Whether there is an error condition</summary>
    [JsonPropertyName("has_error")]
    public bool? HasError { get; set; }
}

/// <summary>
/// Detailed output information from Shelly relay
/// </summary>
public class ShellyRelayOutput
{
    /// <summary>Whether the relay is on</summary>
    [JsonPropertyName("on")]
    public bool? On { get; set; }
    
    /// <summary>Active power in watts</summary>
    [JsonPropertyName("apower")]
    public double? Apower { get; set; }
    
    /// <summary>Voltage in volts</summary>
    [JsonPropertyName("voltage")]
    public double? Voltage { get; set; }
    
    /// <summary>Current in amps</summary>
    [JsonPropertyName("current")]
    public double? Current { get; set; }
    
    /// <summary>Whether overpower condition exists</summary>
    [JsonPropertyName("overpower")]
    public bool? Overpower { get; set; }
    
    /// <summary>Whether overtemperature condition exists</summary>
    [JsonPropertyName("overtemperature")]
    public bool? Overtemperature { get; set; }
}

/// <summary>
/// Command payload for Shelly MQTT messages
/// </summary>
public class ShellyCommandPayload
{
    /// <summary>Command to send: "on" or "off"</summary>
    [JsonPropertyName("turn")]
    public string Turn { get; set; } = string.Empty;

    /// <summary>Optional timer value in seconds</summary>
    [JsonPropertyName("timer")]
    public int? Timer { get; set; }
}
