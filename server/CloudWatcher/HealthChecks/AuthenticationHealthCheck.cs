using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CloudWatcher.HealthChecks
{
    public class AuthenticationHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationHealthCheck> _logger;

        public AuthenticationHealthCheck(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AuthenticationHealthCheck> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var authority = _configuration["Authentication:Authority"];
            if (string.IsNullOrWhiteSpace(authority))
            {
                _logger.LogWarning("Authentication authority not configured");
                return HealthCheckResult.Degraded("Authentication authority not configured");
            }

            var timeoutSeconds = _configuration.GetValue<int>("HealthCheck:AuthServiceTimeoutSeconds", 5);
            var client = _httpClientFactory.CreateClient("health-auth");
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            var metadataUrl = $"{authority.TrimEnd('/')}/.well-known/openid-configuration";

            try
            {
                using var response = await client.GetAsync(metadataUrl, cancellationToken);

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return HealthCheckResult.Healthy("Authentication metadata reachable");
                }

                return HealthCheckResult.Degraded($"Authentication metadata responded with status {(int)response.StatusCode} ({response.StatusCode})");
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Authentication metadata timeout after {TimeoutSeconds}s", timeoutSeconds);
                return HealthCheckResult.Degraded($"Authentication metadata timeout after {timeoutSeconds}s");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication metadata check failed");
                return HealthCheckResult.Unhealthy($"Authentication metadata check failed: {ex.Message}", ex);
            }
        }
    }
}