using System.Net.Http.Headers;
using CloudWatcher.Tests.Fixtures;
using Xunit;

namespace CloudWatcher.Tests.Integration;

/// <summary>
/// Base class for integration tests.
/// Uses TestWebApplicationFactory which is preconfigured for testing with
/// in-memory database and JWT token support.
/// </summary>
public class IntegrationTestBase : IClassFixture<TestWebApplicationFactory>
{
    protected readonly TestWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    public IntegrationTestBase(TestWebApplicationFactory factory)
    {
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Client = Factory.CreateClient();
    }

    /// <summary>
    /// Set authorization token on HttpClient for authenticated requests.
    /// </summary>
    protected void SetAuthorizationToken(string? userId = null, string? email = null)
    {
        var token = TestJwtTokenBuilder.GenerateToken(
            userId ?? "test-user-id",
            email ?? "test@example.com"
        );
        Client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }
}
