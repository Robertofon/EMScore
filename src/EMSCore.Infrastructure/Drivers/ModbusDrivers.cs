using System.Net.Sockets;
using System.Threading.Channels;
using EMSCore.Domain.Interfaces;
using EMSCore.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EMSCore.Infrastructure.Drivers;

public class ModbusRtuDriver : IConfigurableDriver
{
    private readonly ILogger<ModbusRtuDriver> _logger;
    private byte _slaveAddress;
    private DriverConfiguration _config = new();
    private CancellationTokenSource? _eventCts;
    private readonly Channel<DriverEvent> _eventChannel;
    private bool _isConnected;

    public string Name => "Modbus RTU";
    public string Protocol => "Modbus RTU";
    public DriverState State { get; private set; } = DriverState.Disconnected;
    public bool IsConnected => _isConnected && State == DriverState.Connected;

    public ModbusRtuDriver(ILogger<ModbusRtuDriver> logger)
    {
        _logger = logger;
        _eventChannel = Channel.CreateUnbounded<DriverEvent>();
    }

    public Task<bool> ConnectAsync(DriverConfiguration config, CancellationToken ct = default)
    {
        try
        {
            State = DriverState.Connecting;
            
            var portName = config.Parameters.GetValueOrDefault("Port", "COM1");
            _slaveAddress = byte.Parse(config.Parameters.GetValueOrDefault("SlaveAddress", "1"));
            _config = config;

            _isConnected = true;
            State = DriverState.Connected;
            
            _eventCts = new CancellationTokenSource();
            _ = EventStreamWorker(_eventCts.Token);
            
            _logger.LogInformation("Modbus RTU Driver initialized for {Port} (simulation mode)", portName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Modbus RTU driver");
            State = DriverState.Error;
            return Task.FromResult(false);
        }
    }

    public Task ConfigureAsync(DriverConfiguration config, CancellationToken ct = default)
    {
        _config = config;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _eventCts?.Cancel();
        _isConnected = false;
        State = DriverState.Disconnected;
        _logger.LogInformation("Disconnected from Modbus RTU device");
        return Task.CompletedTask;
    }

    public Task<IEnumerable<DriverRegister>> ReadRegistersAsync(
        IEnumerable<string> addresses, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Not connected to Modbus device");
            return Task.FromResult(Enumerable.Empty<DriverRegister>());
        }

        var results = new List<DriverRegister>();
        
        foreach (var address in addresses)
        {
            results.Add(new DriverRegister
            {
                Address = address,
                Type = RegisterType.HoldingRegister,
                Value = GenerateSimulatedValue(address),
                Quality = QualityFlag.Good,
                Timestamp = DateTime.UtcNow
            });
        }

        return Task.FromResult<IEnumerable<DriverRegister>>(results);
    }

    private static object GenerateSimulatedValue(string address)
    {
        var addr = int.Parse(address.Split(':')[0]);
        return (ushort)(addr * 10 + 100);
    }

    public Task WriteRegistersAsync(IEnumerable<DriverRegister> registers, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Not connected to Modbus device");
            return Task.CompletedTask;
        }

        foreach (var register in registers)
        {
            _logger.LogDebug("Writing register {Address} = {Value}", register.Address, register.Value);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<RegisterDefinition>> DiscoverRegistersAsync(CancellationToken ct = default)
    {
        var registers = new List<RegisterDefinition>();
        
        for (ushort i = 0; i < 20; i++)
        {
            registers.Add(new RegisterDefinition
            {
                Address = i.ToString(),
                Name = $"Register_{i}",
                Type = RegisterType.HoldingRegister,
                Description = $"Simulated register at address {i}",
                Unit = "V"
            });
        }

        return Task.FromResult<IEnumerable<RegisterDefinition>>(registers);
    }

    public Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
    {
        return Task.FromResult(IsConnected);
    }

    public async IAsyncEnumerable<DriverEvent> EventStream([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var evt in _eventChannel.Reader.ReadAllAsync(ct))
        {
            yield return evt;
        }
    }

    private async Task EventStreamWorker(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (IsConnected)
            {
                await _eventChannel.Writer.WriteAsync(new DriverEvent
                {
                    EventType = "Heartbeat",
                    Timestamp = DateTime.UtcNow,
                    Message = "Connection OK"
                }, ct);
            }
            
            try
            {
                await Task.Delay(10000, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

public class ModbusTcpDriver : IConfigurableDriver
{
    private readonly ILogger<ModbusTcpDriver> _logger;
    private byte _slaveAddress;
    private DriverConfiguration _config = new();
    private CancellationTokenSource? _eventCts;
    private readonly Channel<DriverEvent> _eventChannel;
    private bool _isConnected;

    public string Name => "Modbus TCP";
    public string Protocol => "Modbus TCP";
    public DriverState State { get; private set; } = DriverState.Disconnected;
    public bool IsConnected => _isConnected && State == DriverState.Connected;

    public ModbusTcpDriver(ILogger<ModbusTcpDriver> logger)
    {
        _logger = logger;
        _eventChannel = Channel.CreateUnbounded<DriverEvent>();
    }

    public Task<bool> ConnectAsync(DriverConfiguration config, CancellationToken ct = default)
    {
        try
        {
            State = DriverState.Connecting;
            
            var host = config.Parameters.GetValueOrDefault("Host", "localhost");
            var port = int.Parse(config.Parameters.GetValueOrDefault("Port", "502"));
            _slaveAddress = byte.Parse(config.Parameters.GetValueOrDefault("SlaveAddress", "1"));
            _config = config;

            _isConnected = true;
            State = DriverState.Connected;
            
            _eventCts = new CancellationTokenSource();
            _ = EventStreamWorker(_eventCts.Token);
            
            _logger.LogInformation("Modbus TCP Driver initialized for {Host}:{Port} (simulation mode)", host, port);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Modbus TCP driver");
            State = DriverState.Error;
            return Task.FromResult(false);
        }
    }

    public Task ConfigureAsync(DriverConfiguration config, CancellationToken ct = default)
    {
        _config = config;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _eventCts?.Cancel();
        _isConnected = false;
        State = DriverState.Disconnected;
        _logger.LogInformation("Disconnected from Modbus TCP device");
        return Task.CompletedTask;
    }

    public Task<IEnumerable<DriverRegister>> ReadRegistersAsync(
        IEnumerable<string> addresses, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Not connected to Modbus device");
            return Task.FromResult(Enumerable.Empty<DriverRegister>());
        }

        var results = new List<DriverRegister>();
        
        foreach (var address in addresses)
        {
            results.Add(new DriverRegister
            {
                Address = address,
                Type = RegisterType.HoldingRegister,
                Value = GenerateSimulatedValue(address),
                Quality = QualityFlag.Good,
                Timestamp = DateTime.UtcNow
            });
        }

        return Task.FromResult<IEnumerable<DriverRegister>>(results);
    }

    private static object GenerateSimulatedValue(string address)
    {
        var addr = int.Parse(address.Split(':')[0]);
        return (ushort)(addr * 10 + 100);
    }

    public Task WriteRegistersAsync(IEnumerable<DriverRegister> registers, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Not connected to Modbus device");
            return Task.CompletedTask;
        }

        foreach (var register in registers)
        {
            _logger.LogDebug("Writing register {Address} = {Value}", register.Address, register.Value);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<RegisterDefinition>> DiscoverRegistersAsync(CancellationToken ct = default)
    {
        var registers = new List<RegisterDefinition>();
        
        for (ushort i = 0; i < 20; i++)
        {
            registers.Add(new RegisterDefinition
            {
                Address = i.ToString(),
                Name = $"Register_{i}",
                Type = RegisterType.HoldingRegister,
                Description = $"Simulated register at address {i}",
                Unit = "W"
            });
        }

        return Task.FromResult<IEnumerable<RegisterDefinition>>(registers);
    }

    public Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
    {
        return Task.FromResult(IsConnected);
    }

    public async IAsyncEnumerable<DriverEvent> EventStream([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var evt in _eventChannel.Reader.ReadAllAsync(ct))
        {
            yield return evt;
        }
    }

    private async Task EventStreamWorker(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (IsConnected)
            {
                await _eventChannel.Writer.WriteAsync(new DriverEvent
                {
                    EventType = "Heartbeat",
                    Timestamp = DateTime.UtcNow,
                    Message = "Connection OK"
                }, ct);
            }
            
            try
            {
                await Task.Delay(10000, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
