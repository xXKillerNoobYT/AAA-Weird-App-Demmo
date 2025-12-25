using Microsoft.AspNetCore.Mvc;
using CloudWatcher.Services;
using CloudWatcher.WebSockets;
using Microsoft.Extensions.Logging;

namespace CloudWatcher.Controllers
{
    /// <summary>
    /// WebSocket endpoint controller for real-time device notifications.
    /// Handles WebSocket connections from devices and routes real-time messages.
    /// </summary>
    [ApiController]
    [Route("ws")]
    public class WebSocketController : ControllerBase
    {
        private readonly WebSocketConnectionPool _connectionPool;
        private readonly ILogger<WebSocketController> _logger;

        public WebSocketController(
            WebSocketConnectionPool connectionPool,
            ILogger<WebSocketController> logger)
        {
            _connectionPool = connectionPool ?? throw new ArgumentNullException(nameof(connectionPool));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// WebSocket endpoint for device real-time notifications.
        /// </summary>
        /// <param name="deviceId">The device identifier</param>
        /// <remarks>
        /// WebSocket endpoint that maintains a persistent connection for a device.
        /// Receives request status updates, response notifications, and other real-time events.
        /// 
        /// Message Format:
        /// {
        ///   "type": "request_update|response_ready|error|heartbeat",
        ///   "requestId": "uuid",
        ///   "status": "pending|processing|completed|failed",
        ///   "data": {},
        ///   "timestamp": "ISO8601"
        /// }
        /// </remarks>
        [HttpGet("devices/{deviceId}")]
        public async Task HandleDeviceWebSocketAsync(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                _logger.LogWarning("WebSocket request with empty deviceId");
                HttpContext.Response.StatusCode = 400;
                return;
            }

            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                _logger.LogWarning("Non-WebSocket request to WebSocket endpoint for device {DeviceId}", deviceId);
                HttpContext.Response.StatusCode = 400;
                return;
            }

            try
            {
                using (var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync())
                {
                    // Create handler for this connection
                    var handler = new WebSocketHandler(webSocket, deviceId, HttpContext.RequestServices.GetRequiredService<ILogger<WebSocketHandler>>());
                    
                    // Add to connection pool
                    _connectionPool.AddConnection(handler);

                    try
                    {
                        // Handle the connection
                        await handler.HandleConnectionAsync();
                    }
                    finally
                    {
                        // Remove from pool when done
                        await _connectionPool.RemoveConnectionAsync(deviceId, handler);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket for device {DeviceId}", deviceId);
                HttpContext.Response.StatusCode = 500;
            }
        }

        /// <summary>
        /// Health check endpoint for WebSocket connectivity.
        /// </summary>
        /// <remarks>
        /// Returns current WebSocket connection statistics.
        /// </remarks>
        [HttpGet("health")]
        [Produces("application/json")]
        public ActionResult<WebSocketHealthResponse> GetWebSocketHealth()
        {
            var devices = _connectionPool.GetConnectedDevices();
            var totalConnections = _connectionPool.GetTotalConnectionCount();

            return Ok(new WebSocketHealthResponse
            {
                Status = "healthy",
                ConnectedDevices = devices.Count,
                TotalConnections = totalConnections,
                DeviceIds = devices,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Response model for WebSocket health endpoint.
    /// </summary>
    public class WebSocketHealthResponse
    {
        /// <summary>
        /// Health status of the WebSocket service
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Number of connected devices
        /// </summary>
        public int ConnectedDevices { get; set; }

        /// <summary>
        /// Total number of active WebSocket connections
        /// </summary>
        public int TotalConnections { get; set; }

        /// <summary>
        /// List of device IDs with active connections
        /// </summary>
        public List<string> DeviceIds { get; set; }

        /// <summary>
        /// Timestamp of health check
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
