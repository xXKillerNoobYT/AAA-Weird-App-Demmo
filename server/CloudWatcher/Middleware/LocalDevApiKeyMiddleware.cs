using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CloudWatcher.Middleware
{
    /// Development-only API key middleware to enable local testing without cloud auth.
    /// Activates only when ASPNETCORE_ENVIRONMENT=Development and header X-Api-Key matches configuration.
    public class LocalDevApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LocalDevApiKeyMiddleware> _logger;
        private const string HeaderName = "X-Api-Key";

        public LocalDevApiKeyMiddleware(RequestDelegate next, ILogger<LocalDevApiKeyMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var env = context.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            var configuredKey = env?.GetValue<string>("LocalAuth:ApiKey");

            _logger.LogInformation("LocalDevApiKeyMiddleware invoked. ConfiguredKey: {ConfiguredKey}", 
                string.IsNullOrEmpty(configuredKey) ? "(not set)" : "(set)");

            if (!string.IsNullOrEmpty(configuredKey) && context.Request.Headers.TryGetValue(HeaderName, out var providedKey))
            {
                _logger.LogInformation("X-Api-Key header found: {ProvidedKey}", providedKey.ToString());
                
                if (providedKey == configuredKey)
                {
                    var identity = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "dev-local"),
                        new Claim(ClaimTypes.Email, "dev@local"),
                        new Claim(ClaimTypes.Role, "admin")
                    }, authenticationType: "LocalDevApiKey");

                    context.User = new ClaimsPrincipal(identity);
                    _logger.LogInformation("LocalDevApiKeyMiddleware: Injected dev identity via X-Api-Key");
                }
                else
                {
                    _logger.LogWarning("X-Api-Key mismatch. Expected: {Expected}, Got: {Got}", 
                        configuredKey, providedKey.ToString());
                }
            }
            else
            {
                _logger.LogInformation("X-Api-Key header not found or config key not set");
            }

            await _next(context);
        }
    }
}