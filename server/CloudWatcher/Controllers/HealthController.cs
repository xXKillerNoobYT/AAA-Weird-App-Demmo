using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CloudWatcher.Data;
using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Net.Http;

namespace CloudWatcher.Controllers
{
    /// <summary>
    /// Health check endpoint for monitoring service status.
    /// </summary>
    [ApiController]
    [Route("health")]
    [Route("api/v2/health")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly CloudWatcherContext _dbContext;
        private readonly ILogger<HealthController> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public HealthController(
            CloudWatcherContext dbContext, 
            ILogger<HealthController> logger,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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

            // Check authentication service availability
            try
            {
                var authority = _configuration["Authentication:Authority"];
                if (string.IsNullOrEmpty(authority))
                {
                    _logger.LogWarning("Health check: Authentication Authority not configured");
                    response.Authentication = "not-configured";
                }
                else
                {
                    // Read timeout from configuration, default to 5 seconds
                    var timeoutSeconds = _configuration.GetValue<int>("HealthCheck:AuthServiceTimeoutSeconds", 5);
                    using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    {
                        try
                        {
                            // Try to ping the OAuth2 metadata endpoint first (more reliable)
                            var metadataUrl = $"{authority.TrimEnd('/')}/.well-known/openid-configuration";
                            _logger.LogDebug($"Health check: Pinging auth metadata endpoint: {metadataUrl}");
                            
                            var authResponse = await _httpClient.GetAsync(metadataUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cts.Token);
                            
                            if (authResponse.IsSuccessStatusCode)
                            {
                                response.Authentication = "available";
                                _logger.LogInformation("Health check: Authentication service available (metadata endpoint accessible)");
                            }
                            else if (authResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            {
                                // Unauthorized is acceptable for metadata endpoint
                                response.Authentication = "available";
                                _logger.LogInformation("Health check: Authentication service available (401 response acceptable)");
                            }
                            else
                            {
                                response.Authentication = "unavailable";
                                _logger.LogWarning($"Health check: Authentication service unavailable (status: {authResponse.StatusCode})");
                            }
                        }
                        catch (System.Threading.Tasks.TaskCanceledException)
                        {
                            _logger.LogWarning($"Health check: Authentication service ping timeout ({timeoutSeconds}s)");
                            response.Authentication = "timeout";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health check: Failed to check authentication service");
                response.Authentication = "error";
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

        /// <summary>
        /// GET /health/live
        /// Liveness probe - confirms process is running
        /// </summary>
        /// <returns>200 OK if process is alive</returns>
        [HttpGet("live")]
        public ActionResult Alive()
        {
            _logger.LogInformation("Liveness check requested");
            return Ok();
        }

        /// <summary>
        /// GET /health/ready
        /// Readiness probe - confirms ready to serve traffic
        /// </summary>
        /// <returns>200 OK if ready</returns>
        [HttpGet("ready")]
        public async Task<ActionResult<HealthResponse>> Ready()
        {
            _logger.LogInformation("Readiness check requested");

            var response = new HealthResponse
            {
                Status = "ready",
                Timestamp = DateTime.UtcNow,
                Version = GetAssemblyVersion()
            };

            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();
                response.Database = canConnect ? "connected" : "disconnected";

                if (!canConnect)
                {
                    _logger.LogWarning("Readiness check failed: Database not connected");
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Readiness check failed");
                response.Status = "not-ready";
                response.Database = "error";
                return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
            }
        }

        /// <summary>
        /// GET /health-legacy
        /// Legacy health endpoint for backward compatibility
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet("/health-legacy")]
        public async Task<ActionResult<object>> HealthLegacy()
        {
            _logger.LogInformation("Legacy health check requested");

            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();
                return Ok(new
                {
                    status = canConnect ? "healthy" : "unhealthy",
                    timestamp = DateTime.UtcNow,
                    service = "CloudWatcher",
                    version = GetAssemblyVersion(),
                    database = canConnect ? "connected" : "disconnected"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Legacy health check failed");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    service = "CloudWatcher",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// GET /info
        /// Service information endpoint
        /// </summary>
        /// <returns>Service metadata</returns>
        [HttpGet("/info")]
        public ActionResult<object> Info()
        {
            _logger.LogInformation("Service info requested");
            return Ok(new
            {
                serviceName = "CloudWatcher API",
                version = GetAssemblyVersion(),
                environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                timestamp = DateTime.UtcNow,
                features = new[]
                {
                    "OAuth2 Authentication",
                    "JWT Token Validation",
                    "Role-Based Access Control",
                    "Request/Response Processing",
                    "Cloud Storage Integration"
                }
            });
        }

        private string GetAssemblyVersion()
        {
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "1.0.0";
            return version;
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
        /// Authentication service status: available, unavailable, error, not-configured, timeout
        /// </summary>
        public string Authentication { get; set; } = "unknown";

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
