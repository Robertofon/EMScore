namespace EMSCore.Domain.Interfaces;

public enum DriverState
{
    Disconnected,
    Connecting,
    Connected,
    Error,
    Reconnecting
}

public enum RegisterType
{
    Coil,
    DiscreteInput,
    HoldingRegister,
    InputRegister
}

public class DriverConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public int TimeoutMs { get; set; } = 5000;
    public int RetryCount { get; set; } = 3;
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class DriverRegister
{
    public string Address { get; set; } = string.Empty;
    public RegisterType Type { get; set; }
    public object? Value { get; set; }
    public DateTime Timestamp { get; set; }
    public Enums.QualityFlag Quality { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DriverEvent
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

public interface IDriver
{
    string Name { get; }
    string Protocol { get; }
    DriverState State { get; }
    bool IsConnected { get; }
    
    Task<bool> ConnectAsync(DriverConfiguration config, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    
    Task<IEnumerable<DriverRegister>> ReadRegistersAsync(IEnumerable<string> addresses, CancellationToken ct = default);
    Task WriteRegistersAsync(IEnumerable<DriverRegister> registers, CancellationToken ct = default);
    
    IAsyncEnumerable<DriverEvent> EventStream(CancellationToken ct = default);
}
