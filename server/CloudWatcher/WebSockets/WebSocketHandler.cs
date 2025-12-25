using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CloudWatcher.WebSockets
{
    /// <summary>
    /// Manages individual WebSocket connections and message handling.
    /// Responsible for receiving and sending messages over a single WebSocket connection.
    /// </summary>
    public class WebSocketHandler
    {
        private readonly WebSocket _webSocket;
        private readonly string _deviceId;
        private readonly ILogger<WebSocketHandler> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private DateTime _lastHeartbeat;
        private DateTime _createdAt;

        public string DeviceId => _deviceId;
        public DateTime CreatedAt => _createdAt;
        public DateTime LastHeartbeat => _lastHeartbeat;
        public bool IsConnected => _webSocket.State == WebSocketState.Open;

        /// <summary>
        /// Initializes a new WebSocket handler for a specific device.
        /// </summary>
        /// <param name="webSocket">The WebSocket connection</param>
        /// <param name="deviceId">The device identifier</param>
        /// <param name="logger">Logger instance</param>
        public WebSocketHandler(WebSocket webSocket, string deviceId, ILogger<WebSocketHandler> logger)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cancellationTokenSource = new CancellationTokenSource();
            _lastHeartbeat = DateTime.UtcNow;
            _createdAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Starts listening for messages on the WebSocket.
        /// This method should be called in a background task.
        /// </summary>
        public async Task HandleConnectionAsync()
        {
            _logger.LogInformation("WebSocket connection opened for device {DeviceId}", 
                _deviceId);

            byte[] buffer = new byte[1024 * 4];

            try
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket close message received from device {DeviceId}", _deviceId);
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            CancellationToken.None);
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        _lastHeartbeat = DateTime.UtcNow;
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogDebug("Message received from device {DeviceId}: {Message}", _deviceId, message);
                        
                        // Message handling happens in WebSocketConnectionPool
                        // This handler just receives and marks activity
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        _logger.LogWarning("Binary message received from device {DeviceId}, ignoring", _deviceId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("WebSocket operation cancelled for device {DeviceId}", _deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WebSocket handler for device {DeviceId}", _deviceId);
            }
            finally
            {
                await CloseConnectionAsync();
            }
        }

        /// <summary>
        /// Sends a message to the connected device.
        /// </summary>
        /// <param name="message">The message object to send</param>
        /// <returns>True if send was successful, false if connection closed</returns>
        public async Task<bool> SendMessageAsync(object message)
        {
            if (!IsConnected)
            {
                _logger.LogWarning("Attempted to send message to disconnected device {DeviceId}", _deviceId);
                return false;
            }

            try
            {
                string json = JsonSerializer.Serialize(message);
                byte[] messageBytes = Encoding.UTF8.GetBytes(json);

                await _webSocket.SendAsync(
                    new ArraySegment<byte>(messageBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

                _logger.LogDebug("Message sent to device {DeviceId}", _deviceId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to device {DeviceId}", _deviceId);
                return false;
            }
        }

        /// <summary>
        /// Sends a heartbeat ping message to keep the connection alive.
        /// </summary>
        public async Task<bool> SendHeartbeatAsync()
        {
            var heartbeatMessage = new
            {
                type = "heartbeat",
                timestamp = DateTime.UtcNow.ToString("O")
            };

            return await SendMessageAsync(heartbeatMessage);
        }

        /// <summary>
        /// Closes the WebSocket connection gracefully.
        /// </summary>
        public async Task CloseConnectionAsync()
        {
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed",
                        CancellationToken.None);
                }

                _webSocket?.Dispose();
                _logger.LogInformation("WebSocket connection closed for device {DeviceId}", _deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing WebSocket connection for device {DeviceId}", _deviceId);
            }
        }

        /// <summary>
        /// Cancels the current operation and closes the connection.
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Checks if the connection has been idle for too long.
        /// </summary>
        /// <param name="idleTimeoutSeconds">Timeout in seconds</param>
        /// <returns>True if connection is idle</returns>
        public bool IsIdleForSeconds(int idleTimeoutSeconds)
        {
            return (DateTime.UtcNow - _lastHeartbeat).TotalSeconds > idleTimeoutSeconds;
        }
    }
}
