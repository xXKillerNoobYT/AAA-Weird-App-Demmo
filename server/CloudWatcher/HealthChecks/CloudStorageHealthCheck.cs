using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CloudWatcher.HealthChecks
{
    public class CloudStorageHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CloudStorageHealthCheck> _logger;

        public CloudStorageHealthCheck(IConfiguration configuration, ILogger<CloudStorageHealthCheck> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var provider = _configuration["CloudStorage:Provider"] ?? "Local";

            // Only validate local storage path for now; other providers can extend later
            if (!provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(HealthCheckResult.Healthy($"Cloud storage provider '{provider}' configured"));
            }

            var localPath = _configuration["CloudStorage:LocalPath"] ?? "../Cloud";
            var absolutePath = Path.GetFullPath(localPath, Directory.GetCurrentDirectory());

            try
            {
                Directory.CreateDirectory(absolutePath);
                Directory.CreateDirectory(Path.Combine(absolutePath, "Requests"));
                Directory.CreateDirectory(Path.Combine(absolutePath, "Responses"));

                return Task.FromResult(HealthCheckResult.Healthy($"Cloud storage reachable at '{absolutePath}'"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloud storage path not accessible at {Path}", absolutePath);
                return Task.FromResult(HealthCheckResult.Unhealthy($"Cloud storage path not accessible at '{absolutePath}': {ex.Message}"));
            }
        }
    }
}