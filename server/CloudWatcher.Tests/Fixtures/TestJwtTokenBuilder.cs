using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CloudWatcher.Tests.Fixtures;

/// <summary>
/// Helper to generate JWT tokens for integration tests.
/// Creates valid JWTs that the test server can validate.
/// </summary>
public class TestJwtTokenBuilder
{
    private const string TestSecret = "test-secret-key-that-is-longer-than-256-bits-minimum-for-validation";
    private const string TestIssuer = "https://test.cloudwatcher.local";
    private const string TestAudience = "test-api";
    
    /// <summary>
    /// Generates a valid JWT token for testing.
    /// </summary>
    public static string GenerateToken(
        string userId = "test-user-id",
        string email = "test@example.com",
        IEnumerable<string>? roles = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim("sub", userId),
            new Claim("oid", userId) // Azure AD object ID
        };

        if (roles != null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Gets the test secret key used for token signing.
    /// </summary>
    public static string GetTestSecret() => TestSecret;

    /// <summary>
    /// Gets the test issuer.
    /// </summary>
    public static string GetTestIssuer() => TestIssuer;

    /// <summary>
    /// Gets the test audience.
    /// </summary>
    public static string GetTestAudience() => TestAudience;
}
