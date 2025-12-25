using System.Collections.Concurrent;
using CloudWatcher.WebSockets;
using Microsoft.Extensions.Logging;

namespace CloudWatcher.Services
{
    /// <summary>
    /// Manages WebSocket connections for all connected devices.
    /// Maintains a thread-safe pool of connections organized by device ID.
    /// </summary>
    public class WebSocketConnectionPool : IAsyncDisposable
    {
        private readonly ConcurrentDictionary<string, List<WebSocketHandler>> _connections;
        private readonly ILogger<WebSocketConnectionPool> _logger;
        private readonly object _lockObject = new object();
        private Timer _heartbeatTimer;

        public WebSocketConnectionPool(ILogger<WebSocketConnectionPool> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connections = new ConcurrentDictionary<string, List<WebSocketHandler>>();

            // Start heartbeat timer
            _heartbeatTimer = new Timer(
                SendHeartbeats,
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Adds a new WebSocket handler to the connection pool.
        /// </summary>
        /// <param name="handler">The WebSocket handler to add</param>
        public void AddConnection(WebSocketHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lockObject)
            {
                var newHandler = new List<WebSocketHandler>();
                newHandler.Add(handler);

                _connections.AddOrUpdate(handler.DeviceId, newHandler, (key, existingList) =>
                {
                    existingList.Add(handler);
                    return existingList;
                });

                _logger.LogInformation(
                    "WebSocket connection added for device {DeviceId}. Total connections for device: {Count}",
                    handler.DeviceId,
                    _connections[handler.DeviceId].Count);
            }
        }

        /// <summary>
        /// Removes a WebSocket handler from the connection pool.
        /// </summary>
        /// <param name="deviceId">The device ID</param>
        /// <param name="handler">The handler to remove</param>
        public async Task RemoveConnectionAsync(string deviceId, WebSocketHandler handler)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentNullException(nameof(deviceId));

            lock (_lockObject)
            {
                if (_connections.TryGetValue(deviceId, out var handlers))
                {
                    handlers.Remove(handler);

                    if (handlers.Count == 0)
                    {
                        _connections.TryRemove(deviceId, out _);
                        _logger.LogInformation("Last connection removed for device {DeviceId}", deviceId);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Connection removed for device {DeviceId}. Remaining: {Count}",
                            deviceId,
                            handlers.Count);
                    }
                }
            }

            await handler.CloseConnectionAsync();
        }

        /// <summary>
        /// Gets all active connections for a specific device.
        /// </summary>
        /// <param name="deviceId">The device ID</param>
        /// <returns>List of WebSocket handlers for the device</returns>
        public List<WebSocketHandler> GetConnectionsForDevice(string deviceId)
        {
            if (_connections.TryGetValue(deviceId, out var handlers))
            {
                return new List<WebSocketHandler>(handlers.Where(h => h.IsConnected).ToList());
            }
            return new List<WebSocketHandler>();
        }

        /// <summary>
        /// Gets the count of active connections for a device.
        /// </summary>
        /// <param name="deviceId">The device ID</param>
        /// <returns>Number of active connections</returns>
        public int GetConnectionCountForDevice(string deviceId)
        {
            return GetConnectionsForDevice(deviceId).Count;
        }

        /// <summary>
        /// Gets all connected devices.
        /// </summary>
        /// <returns>List of device IDs with active connections</returns>
        public List<string> GetConnectedDevices()
        {
            return _connections.Keys.ToList();
        }

        /// <summary>
        /// Gets total connection count across all devices.
        /// </summary>
        public int GetTotalConnectionCount()
        {
            lock (_lockObject)
            {
                return _connections.Values.Sum(list => list.Count(h => h.IsConnected));
            }
        }

        /// <summary>
        /// Broadcasts a message to all connections of a specific device.
        /// </summary>
        /// <param name="deviceId">The device ID</param>
        /// <param name="message">The message to send</param>
        /// <returns>Number of successful sends</returns>
        public async Task<int> BroadcastToDeviceAsync(string deviceId, object message)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentNullException(nameof(deviceId));

            var handlers = GetConnectionsForDevice(deviceId);
            int successCount = 0;

            foreach (var handler in handlers)
            {
                if (await handler.SendMessageAsync(message))
                {
                    successCount++;
                }
            }

            if (successCount > 0)
            {
                _logger.LogDebug(
                    "Message broadcast to device {DeviceId}: {SuccessCount}/{TotalCount} connections",
                    deviceId,
                    successCount,
                    handlers.Count);
            }

            return successCount;
        }

        /// <summary>
        /// Broadcasts a message to all connected devices.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns>Dictionary mapping device IDs to number of successful sends</returns>
        public async Task<Dictionary<string, int>> BroadcastToAllAsync(object message)
        {
            var results = new Dictionary<string, int>();
            var devices = GetConnectedDevices();

            foreach (var deviceId in devices)
            {
                var count = await BroadcastToDeviceAsync(deviceId, message);
                if (count > 0)
                {
                    results[deviceId] = count;
                }
            }

            _logger.LogDebug("Broadcast message sent to {DeviceCount} devices", results.Count);
            return results;
        }

        /// <summary>
        /// Cleans up idle and disconnected connections.
        /// </summary>
        /// <param name="idleTimeoutSeconds">Idle timeout in seconds (default: 300)</param>
        public async Task CleanupIdleConnectionsAsync(int idleTimeoutSeconds = 300)
        {
            var devicesToClean = GetConnectedDevices();

            foreach (var deviceId in devicesToClean)
            {
                var handlers = GetConnectionsForDevice(deviceId);
                var handlersToRemove = handlers
                    .Where(h => h.IsIdleForSeconds(idleTimeoutSeconds) || !h.IsConnected)
                    .ToList();

                foreach (var handler in handlersToRemove)
                {
                    await RemoveConnectionAsync(deviceId, handler);
                }
            }

            if (devicesToClean.Count > 0)
            {
                _logger.LogDebug("Cleaned up idle connections. Active connections: {Count}", GetTotalConnectionCount());
            }
        }

        /// <summary>
        /// Sends heartbeat to all connected devices.
        /// </summary>
        private async void SendHeartbeats(object? state)
        {
            try
            {
                var devices = GetConnectedDevices();
                int successCount = 0;

                foreach (var deviceId in devices)
                {
                    var handlers = GetConnectionsForDevice(deviceId);
                    foreach (var handler in handlers)
                    {
                        if (await handler.SendHeartbeatAsync())
                        {
                            successCount++;
                        }
                    }
                }

                if (successCount > 0)
                {
                    _logger.LogDebug("Heartbeat sent to {Count} connections", successCount);
                }

                // Cleanup idle connections every heartbeat cycle
                await CleanupIdleConnectionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeats");
            }
        }

        /// <summary>
        /// Disposes all connections and timers.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            _heartbeatTimer?.Dispose();

            var devices = GetConnectedDevices();
            foreach (var deviceId in devices)
            {
                var handlers = GetConnectionsForDevice(deviceId).ToList();
                foreach (var handler in handlers)
                {
                    await handler.CloseConnectionAsync();
                }
            }

            _connections.Clear();
            _logger.LogInformation("WebSocketConnectionPool disposed");
        }
    }
}
