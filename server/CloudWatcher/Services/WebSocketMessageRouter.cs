using System.Text.Json;
using CloudWatcher.WebSockets;
using Microsoft.Extensions.Logging;

namespace CloudWatcher.Services
{
    /// <summary>
    /// Routes messages to connected WebSocket clients based on device ID and message type.
    /// Handles notification of request status changes, responses, and other real-time events.
    /// </summary>
    public class WebSocketMessageRouter
    {
        private readonly WebSocketConnectionPool _connectionPool;
        private readonly ILogger<WebSocketMessageRouter> _logger;

        public WebSocketMessageRouter(
            WebSocketConnectionPool connectionPool,
            ILogger<WebSocketMessageRouter> logger)
        {
            _connectionPool = connectionPool ?? throw new ArgumentNullException(nameof(connectionPool));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Notifies a device that a request status has changed.
        /// </summary>
        /// <param name="deviceId">The device ID</param>
        /// <param name="requestId">The request ID</param>
        /// <param name="newStatus">The new status</param>
        /// <param name="details">Optional additional details</param>
        public async Task NotifyRequestStatusChangeAsync(
            string deviceId,
            string requestId,
            string newStatus,
            object? details = null)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentNullException(nameof(deviceId));
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentNullException(nameof(requestId));

            var message = new
            {
                type = "request_update",
                requestId = requestId,
                status = newStatus,
                data = details,
                timestamp = DateTime.UtcNow.ToString("O")
            };

            int sentCount = await _connectionPool.BroadcastToDeviceAsync(deviceId, message);
            _logger.LogInformation(
                "Request status notification sent to device {DeviceId}: {RequestId} -> {Status} ({Count} connections)",
                deviceId,
                requestId,
                newStatus,
                sentCount);
        }

        /// <summary>
        /// Notifies a device that a response is ready for retrieval.
        /// </summary>
        /// <param name="deviceId">The device ID</param>
        /// <param name="requestId">The request ID</param>
        /// <param name="responseData">The response data</param>
        public async Task NotifyResponseReadyAsync(
            string deviceId,
            string requestId,
            object responseData)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentNullException(nameof(deviceId));
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentNullException(nameof(requestId));

            var message = new
            {
                type = "response_ready",
                requestId = requestId,
                data = responseData,
                timestamp = DateTime.UtcNow.ToString("O")
            };

            int sentCount = await _connectionPool.BroadcastToDeviceAsync(deviceId, message);
            _logger.LogInformation(
                "Response ready notification sent to device {DeviceId}: {RequestId} ({Count} connections)",
                deviceId,
                requestId,
                sentCount);
        }

        /// <summary>
        /// Notifies a device of an error related to a request.
        /// </summary>
        /// <param name="deviceId">The device ID</param>
        /// <param name="requestId">The request ID</param>
        /// <param name="errorCode">The error code</param>
        /// <param name="errorMessage">The error message</param>
        public async Task NotifyErrorAsync(
            string deviceId,
            string requestId,
            string errorCode,
            string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentNullException(nameof(deviceId));
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentNullException(nameof(requestId));

            var message = new
            {
                type = "error",
                requestId = requestId,
                errorCode = errorCode,
                errorMessage = errorMessage,
                timestamp = DateTime.UtcNow.ToString("O")
            };

            int sentCount = await _connectionPool.BroadcastToDeviceAsync(deviceId, message);
            _logger.LogWarning(
                "Error notification sent to device {DeviceId}: {RequestId} ({ErrorCode}) ({Count} connections)",
                deviceId,
                requestId,
                errorCode,
                sentCount);
        }

        /// <summary>
        /// Sends a generic message to a device.
        /// </summary>
        /// <param name="deviceId">The device ID</param>
        /// <param name="messageType">The message type</param>
        /// <param name="data">The message data</param>
        public async Task SendGenericMessageAsync(
            string deviceId,
            string messageType,
            object? data = null)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentNullException(nameof(deviceId));
            if (string.IsNullOrWhiteSpace(messageType))
                throw new ArgumentNullException(nameof(messageType));

            var message = new
            {
                type = messageType,
                data = data,
                timestamp = DateTime.UtcNow.ToString("O")
            };

            int sentCount = await _connectionPool.BroadcastToDeviceAsync(deviceId, message);
            _logger.LogDebug(
                "Generic message sent to device {DeviceId}: {MessageType} ({Count} connections)",
                deviceId,
                messageType,
                sentCount);
        }

        /// <summary>
        /// Broadcasts a message to all connected devices.
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="data">The message data</param>
        public async Task BroadcastToAllAsync(string messageType, object? data = null)
        {
            if (string.IsNullOrWhiteSpace(messageType))
                throw new ArgumentNullException(nameof(messageType));

            var message = new
            {
                type = messageType,
                data = data,
                timestamp = DateTime.UtcNow.ToString("O")
            };

            var results = await _connectionPool.BroadcastToAllAsync(message);
            _logger.LogDebug(
                "Broadcast message sent to all devices: {MessageType} ({Count} devices)",
                messageType,
                results.Count);
        }

        /// <summary>
        /// Gets connection statistics.
        /// </summary>
        public WebSocketStatistics GetStatistics()
        {
            return new WebSocketStatistics
            {
                TotalConnections = _connectionPool.GetTotalConnectionCount(),
                ConnectedDevices = _connectionPool.GetConnectedDevices().Count,
                SnapshotTime = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Statistics about current WebSocket connections.
    /// </summary>
    public class WebSocketStatistics
    {
        public int TotalConnections { get; set; }
        public int ConnectedDevices { get; set; }
        public DateTime SnapshotTime { get; set; }
    }
}
