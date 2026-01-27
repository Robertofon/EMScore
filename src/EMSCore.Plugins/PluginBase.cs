namespace EMSCore.Plugins;

/// <summary>
/// Base class for all plugins in the EMSCore system
/// </summary>
public abstract class PluginBase
{
    public string Name { get; protected set; } = string.Empty;
    public string Version { get; protected set; } = "1.0.0";
    
    public abstract Task Initialize();
    public abstract Task Shutdown();
}