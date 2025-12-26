using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CloudWatcher.Data;
using CloudWatcher.Tests.Fixtures;

namespace CloudWatcher.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory configured for integration tests.
/// Provides InMemoryDatabase, test JWT configuration, and bypasses real authentication.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Force test environment
        builder.UseEnvironment("Test");

        // Clear the database connection string to force InMemory
        builder.UseSetting("ConnectionStrings:DefaultConnection", string.Empty);

        // Override authentication configuration for testing
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testSettings = new Dictionary<string, string?>()
            {
                // Disable Azure AD for tests
                { "Authentication:Authority", null },
                { "Authentication:Audience", "test-api" },
                { "Authentication:AllowUnauthenticatedRequests", "false" },
                
                // JWT test settings (same as in TestJwtTokenBuilder)
                { "Authentication:Jwt:Secret", TestJwtTokenBuilder.GetTestSecret() },
                { "Authentication:Jwt:Issuer", TestJwtTokenBuilder.GetTestIssuer() },
                { "Authentication:Jwt:Audience", "test-api" },
                { "Authentication:Jwt:ExpirationMinutes", "60" }
            };
            config.AddInMemoryCollection(testSettings);
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration to avoid conflicts
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CloudWatcherContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add InMemoryDatabase for testing
            services.AddDbContext<CloudWatcherContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestDatabase_" + Guid.NewGuid());
            });
        });
    }

    /// <summary>
    /// Override factory disposal to clean up databases.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
