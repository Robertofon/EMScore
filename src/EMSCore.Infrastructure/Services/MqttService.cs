using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using EMSCore.Domain.Interfaces;
using MQTTnet.Server;

namespace EMSCore.Infrastructure.Services;

/// <summary>
/// MQTT service implementation using MQTTnet library
/// Provides reliable MQTT communication with automatic reconnection and message handling
/// </summary>
public class MqttService : IMqttService, IDisposable
{
    private readonly ILogger<MqttService> _logger;
    private readonly MqttConfiguration _config;
    private readonly IManagedMqttClient _mqttClient;
    private readonly ConcurrentDictionary<string, Func<MqttMessage, Task>> _subscriptionHandlers;
    private bool _disposed;

    /// <summary>
    /// Event fired when the connection status changes
    /// </summary>
    public event EventHandler<MqttConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

    /// <summary>
    /// Gets whether the MQTT client is currently connected
    /// </summary>
    public bool IsConnected => _mqttClient.IsConnected;

    /// <summary>
    /// Initializes a new instance of the MqttService
    /// </summary>
    /// <param name="logger">Logger for service operations</param>
    /// <param name="config">MQTT configuration options</param>
    public MqttService(ILogger<MqttService> logger, IOptions<MqttConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
        _subscriptionHandlers = new ConcurrentDictionary<string, Func<MqttMessage, Task>>();

        // Create managed MQTT client for automatic reconnection
        var factory = new MqttFactory();
        _mqttClient = factory.CreateManagedMqttClient();

        // Setup event handlers
        _mqttClient.ConnectedAsync += OnConnectedAsync;
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _mqttClient.ConnectingFailedAsync += OnConnectingFailedAsync;
    }

    /// <summary>
    /// Starts the MQTT service and connects to the broker
    /// Uses managed client for automatic reconnection and message queuing
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting MQTT service, connecting to broker at {Host}:{Port}", 
                _config.BrokerHost, _config.BrokerPort);

            // Build client options
            var clientOptionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(_config.BrokerHost, _config.BrokerPort)
                .WithClientId(_config.ClientId)
                .WithKeepAlivePeriod(_config.KeepAliveInterval)
                .WithCleanSession(false); // Persistent session for reliable delivery

            // Add credentials if provided
            if (!string.IsNullOrEmpty(_config.Username))
            {
                clientOptionsBuilder.WithCredentials(_config.Username, _config.Password);
            }

            // Add TLS if enabled
            if (_config.UseTls)
            {
                clientOptionsBuilder.WithTlsOptions(o =>
                {
                    o.WithSslProtocols(System.Security.Authentication.SslProtocols.Tls12);
                    o.WithAllowUntrustedCertificates(_config.AllowUntrustedCertificates);
                    o.WithIgnoreCertificateChainErrors(_config.IgnoreCertificateChainErrors);
                });
            }

            var clientOptions = clientOptionsBuilder.Build();

            // Configure managed client options
            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(clientOptions)
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithMaxPendingMessages(_config.MaxPendingMessages)
                .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
                .Build();

            // Start the managed client
            await _mqttClient.StartAsync(managedOptions);

            _logger.LogInformation("MQTT service started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MQTT service");
            throw;
        }
    }

    /// <summary>
    /// Stops the MQTT service and disconnects from the broker
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Stopping MQTT service");

            if (_mqttClient.IsStarted)
            {
                await _mqttClient.StopAsync();
            }

            _subscriptionHandlers.Clear();
            _logger.LogInformation("MQTT service stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MQTT service");
            throw;
        }
    }

    /// <summary>
    /// Publishes a message to an MQTT topic with JSON serialization
    /// </summary>
    /// <typeparam name="T">Type of the payload to publish</typeparam>
    /// <param name="topic">MQTT topic to publish to</param>
    /// <param name="payload">Data to publish (will be JSON serialized)</param>
    /// <param name="qos">Quality of Service level (0, 1, or 2)</param>
    /// <param name="retain">Whether the message should be retained by the broker</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task PublishAsync<T>(string topic, T payload, int qos = 1, bool retain = false, CancellationToken cancellationToken = default)
    {
        try
        {
            // Serialize payload to JSON
            var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(jsonPayload)
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
                .WithRetainFlag(retain)
                .Build();

            await _mqttClient.EnqueueAsync(message);

            _logger.LogDebug("Published message to topic {Topic} with QoS {Qos}, retain: {Retain}", 
                topic, qos, retain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to topic {Topic}", topic);
            throw;
        }
    }

    /// <summary>
    /// Subscribes to an MQTT topic with a message handler
    /// Supports MQTT wildcards (+ for single level, # for multi-level)
    /// </summary>
    /// <param name="topicFilter">MQTT topic filter (supports wildcards)</param>
    /// <param name="handler">Function to handle received messages</param>
    /// <param name="qos">Quality of Service level for subscription</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SubscribeAsync(string topicFilter, Func<MqttMessage, Task> handler, int qos = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            // Store the handler for this topic filter
            _subscriptionHandlers.AddOrUpdate(topicFilter, handler, (key, oldValue) => handler);

            // Subscribe to the topic
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(topicFilter).WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos))
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions.TopicFilters);

            _logger.LogInformation("Subscribed to topic filter {TopicFilter} with QoS {Qos}", topicFilter, qos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to topic filter {TopicFilter}", topicFilter);
            throw;
        }
    }

    /// <summary>
    /// Unsubscribes from an MQTT topic
    /// </summary>
    /// <param name="topicFilter">MQTT topic filter to unsubscribe from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UnsubscribeAsync(string topicFilter, CancellationToken cancellationToken = default)
    {
        try
        {
            // Remove the handler
            _subscriptionHandlers.TryRemove(topicFilter, out _);

            // Unsubscribe from the topic
            var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder()
                .WithTopicFilter(topicFilter)
                .Build();

            await _mqttClient.UnsubscribeAsync(unsubscribeOptions.TopicFilters);

            _logger.LogInformation("Unsubscribed from topic filter {TopicFilter}", topicFilter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from topic filter {TopicFilter}", topicFilter);
            throw;
        }
    }

    /// <summary>
    /// Handles MQTT client connected event
    /// </summary>
    private Task OnConnectedAsync(MqttClientConnectedEventArgs e)
    {
        _logger.LogInformation("MQTT client connected to broker");
        ConnectionStatusChanged?.Invoke(this, new MqttConnectionStatusChangedEventArgs(true, "Connected"));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles MQTT client disconnected event
    /// </summary>
    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        var reason = e.Reason.ToString();
        _logger.LogWarning("MQTT client disconnected from broker. Reason: {Reason}", reason);
        ConnectionStatusChanged?.Invoke(this, new MqttConnectionStatusChangedEventArgs(false, reason, e.Exception));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles MQTT connection failed event
    /// </summary>
    private Task OnConnectingFailedAsync(ConnectingFailedEventArgs e)
    {
        _logger.LogError(e.Exception, "MQTT client failed to connect to broker");
        ConnectionStatusChanged?.Invoke(this, new MqttConnectionStatusChangedEventArgs(false, "Connection failed", e.Exception));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles incoming MQTT messages and routes them to appropriate handlers
    /// </summary>
    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage.PayloadSegment.ToArray();
            var payloadString = Encoding.UTF8.GetString(payload);

            _logger.LogDebug("Received message on topic {Topic}, payload length: {Length}", topic, payload.Length);

            // Create message object
            var message = new MqttMessage(
                Topic: topic,
                Payload: payload,
                PayloadAsString: payloadString,
                QualityOfServiceLevel: (int)e.ApplicationMessage.QualityOfServiceLevel,
                Retain: e.ApplicationMessage.Retain,
                Timestamp: DateTime.UtcNow
            );

            // Find matching handlers using topic pattern matching
            var matchingHandlers = _subscriptionHandlers
                .Where(kvp => IsTopicMatch(kvp.Key, topic))
                .Select(kvp => kvp.Value);

            // Execute all matching handlers
            var tasks = matchingHandlers.Select(handler => ExecuteHandlerSafely(handler, message));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing received MQTT message");
        }
    }

    /// <summary>
    /// Executes a message handler safely with error handling
    /// </summary>
    private async Task ExecuteHandlerSafely(Func<MqttMessage, Task> handler, MqttMessage message)
    {
        try
        {
            await handler(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MQTT message handler for topic {Topic}", message.Topic);
        }
    }

    /// <summary>
    /// Checks if a topic matches a topic filter (supports MQTT wildcards)
    /// </summary>
    /// <param name="filter">Topic filter with potential wildcards</param>
    /// <param name="topic">Actual topic to match</param>
    /// <returns>True if topic matches filter</returns>
    private static bool IsTopicMatch(string filter, string topic)
    {
        // Handle exact match
        if (filter == topic)
            return true;

        // Handle multi-level wildcard (#)
        if (filter.EndsWith("/#"))
        {
            var prefix = filter[..^2];
            return topic.StartsWith(prefix + "/") || topic == prefix;
        }

        // Handle single-level wildcard (+)
        if (filter.Contains('+'))
        {
            var filterParts = filter.Split('/');
            var topicParts = topic.Split('/');

            if (filterParts.Length != topicParts.Length)
                return false;

            for (int i = 0; i < filterParts.Length; i++)
            {
                if (filterParts[i] != "+" && filterParts[i] != topicParts[i])
                    return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Disposes the MQTT service and releases resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _mqttClient?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Configuration options for MQTT service
/// </summary>
public class MqttConfiguration
{
    /// <summary>
    /// MQTT broker hostname or IP address
    /// </summary>
    public string BrokerHost { get; set; } = "localhost";

    /// <summary>
    /// MQTT broker port (default: 1883 for non-TLS, 8883 for TLS)
    /// </summary>
    public int BrokerPort { get; set; } = 1883;

    /// <summary>
    /// Username for broker authentication (optional)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password for broker authentication (optional)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Whether to use TLS encryption
    /// </summary>
    public bool UseTls { get; set; } = false;

    /// <summary>
    /// Whether to allow untrusted certificates (for development)
    /// </summary>
    public bool AllowUntrustedCertificates { get; set; } = false;

    /// <summary>
    /// Whether to ignore certificate chain errors (for development)
    /// </summary>
    public bool IgnoreCertificateChainErrors { get; set; } = false;

    /// <summary>
    /// MQTT client identifier (should be unique per client)
    /// </summary>
    public string ClientId { get; set; } = Environment.MachineName + "_" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Keep-alive interval for the connection
    /// </summary>
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Maximum number of pending messages in the queue
    /// </summary>
    public int MaxPendingMessages { get; set; } = 1000;
}