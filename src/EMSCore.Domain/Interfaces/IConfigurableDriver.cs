namespace EMSCore.Domain.Interfaces;

public class RegisterDefinition
{
    public string Address { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public RegisterType Type { get; set; }
    public double ScaleFactor { get; set; } = 1.0;
    public double Offset { get; set; } = 0.0;
    public string? Unit { get; set; }
    public string? Description { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
}

public interface IConfigurableDriver : IDriver
{
    Task ConfigureAsync(DriverConfiguration config, CancellationToken ct = default);
    Task<IEnumerable<RegisterDefinition>> DiscoverRegistersAsync(CancellationToken ct = default);
    Task<bool> ValidateConnectionAsync(CancellationToken ct = default);
}
