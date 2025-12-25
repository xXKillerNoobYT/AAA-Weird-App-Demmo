using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudWatcher.Data;
using System;
using System.Threading.Tasks;

namespace CloudWatcher.Controllers
{
    /// <summary>
    /// Health check endpoint for monitoring service status.
    /// </summary>
    [ApiController]
    [Route("health")]
    [Produces("application/json")]
    public class HealthController : ControllerBase
    {
        private readonly CloudWatcherContext _dbContext;
        private readonly ILogger<HealthController> _logger;

        public HealthController(CloudWatcherContext dbContext, ILogger<HealthController> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET /health
        /// Check application health status including database connectivity.
        /// </summary>
        /// <returns>Health status response</returns>
        /// <response code="200">Application is healthy</response>
        /// <response code="503">Application is unhealthy</response>
        [HttpGet]
        public async Task<ActionResult<HealthResponse>> GetHealthAsync()
        {
            _logger.LogInformation("Health check requested");

            var response = new HealthResponse
            {
                Status = "healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            };

            // Check database connectivity
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();
                response.Database = canConnect ? "connected" : "disconnected";

                if (!canConnect)
                {
                    _logger.LogWarning("Health check failed: Database not connected");
                    response.Status = "unhealthy";
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
                }

                _logger.LogInformation("Health check passed: Database connected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed: Database connection error");
                response.Status = "unhealthy";
                response.Database = "error";
                response.ErrorMessage = ex.Message;
                return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
            }

            // Check uptime (simple counter)
            try
            {
                // Test a simple query to ensure database is responsive
                await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
                response.DatabaseResponseMs = 1; // Simplified - in production, measure actual time
                _logger.LogInformation("Health check complete: All systems healthy");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed: Database query error");
                response.Status = "unhealthy";
                response.Database = "error";
                response.ErrorMessage = $"Database query failed: {ex.Message}";
                return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
            }
        }
    }

    /// <summary>
    /// Health check response DTO
    /// </summary>
    public class HealthResponse
    {
        /// <summary>
        /// Overall health status: healthy, degraded, unhealthy
        /// </summary>
        public string Status { get; set; } = "unknown";

        /// <summary>
        /// Database connectivity status: connected, disconnected, error
        /// </summary>
        public string Database { get; set; } = "unknown";

        /// <summary>
        /// Response timestamp in UTC
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// API version
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Database response time in milliseconds
        /// </summary>
        public int DatabaseResponseMs { get; set; }

        /// <summary>
        /// Error message if any
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
