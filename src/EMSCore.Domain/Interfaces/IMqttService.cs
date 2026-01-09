namespace EMSCore.Domain.Interfaces;

/// <summary>
/// Service interface for MQTT communication
/// Handles publishing and subscribing to MQTT topics for energy data exchange
/// </summary>
public interface IMqttService
{
    /// <summary>
    /// Publishes a message to an MQTT topic
    /// </summary>
    /// <typeparam name="T">Type of the payload to publish</typeparam>
    /// <param name="topic">MQTT topic to publish to</param>
    /// <param name="payload">Data to publish</param>
    /// <param name="qos">Quality of Service level (0, 1, or 2)</param>
    /// <param name="retain">Whether the message should be retained by the broker</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishAsync<T>(string topic, T payload, int qos = 1, bool retain = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to an MQTT topic with a message handler
    /// </summary>
    /// <param name="topicFilter">MQTT topic filter (supports wildcards)</param>
    /// <param name="handler">Function to handle received messages</param>
    /// <param name="qos">Quality of Service level for subscription</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SubscribeAsync(string topicFilter, Func<MqttMessage, Task> handler, int qos = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from an MQTT topic
    /// </summary>
    /// <param name="topicFilter">MQTT topic filter to unsubscribe from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task UnsubscribeAsync(string topicFilter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the MQTT client is currently connected
    /// </summary>
    /// <returns>True if connected, false otherwise</returns>
    bool IsConnected { get; }

    /// <summary>
    /// Starts the MQTT service and connects to the broker
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the MQTT service and disconnects from the broker
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event fired when the connection status changes
    /// </summary>
    event EventHandler<MqttConnectionStatusChangedEventArgs>? ConnectionStatusChanged;
}

/// <summary>
/// Represents an MQTT message received from a topic
/// </summary>
public record MqttMessage(
    string Topic,
    byte[] Payload,
    string PayloadAsString,
    int QualityOfServiceLevel,
    bool Retain,
    DateTime Timestamp
);

/// <summary>
/// Event arguments for MQTT connection status changes
/// </summary>
public class MqttConnectionStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Whether the client is currently connected
    /// </summary>
    public bool IsConnected { get; }

    /// <summary>
    /// Reason for the connection status change
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Exception that caused disconnection (if any)
    /// </summary>
    public Exception? Exception { get; }

    public MqttConnectionStatusChangedEventArgs(bool isConnected, string? reason = null, Exception? exception = null)
    {
        IsConnected = isConnected;
        Reason = reason;
        Exception = exception;
    }
}