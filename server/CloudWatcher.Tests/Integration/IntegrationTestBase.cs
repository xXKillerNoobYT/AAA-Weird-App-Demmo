using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CloudWatcher.Data;
using Xunit;

namespace CloudWatcher.Tests.Integration;

/// <summary>
/// Base class for integration tests that provides WebApplicationFactory setup
/// with an in-memory database for testing.
/// </summary>
public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<CloudWatcherContext>));
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add DbContext using in-memory database for testing
                services.AddDbContext<CloudWatcherContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });

                // Build service provider and ensure database is created
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();
                    db.Database.EnsureCreated();
                }
            });
        });

        Client = Factory.CreateClient();
    }
}
