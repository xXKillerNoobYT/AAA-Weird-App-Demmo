using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CloudWatcher.Auth;

/// <summary>
/// Configures JWT Bearer token validation for Azure AD and OAuth2 providers.
/// Supports both Azure AD and generic OAuth2 token validation.
/// </summary>
public static class JwtTokenConfiguration
{
    /// <summary>
    /// Add JWT authentication with Azure AD and OAuth2 support.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authConfig = configuration.GetSection("Authentication");
        var authority = authConfig["Authority"];
        var audience = authConfig["Audience"];
        var jwtSecret = authConfig["Jwt:Secret"];
        var enableLocalJwtValidation = authConfig.GetValue<bool>("EnableLocalJwtValidation", true);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Authority for token validation (Azure AD, etc.)
                if (!string.IsNullOrEmpty(authority))
                {
                    options.Authority = authority;
                }

                // Audience validation
                if (!string.IsNullOrEmpty(audience))
                {
                    options.Audience = audience;
                }

                // Token validation parameters
                var tokenValidationParams = new TokenValidationParameters
                {
                    // Validate the token signature
                    ValidateIssuerSigningKey = true,
                    
                    // Validate the token issuer (Authority)
                    ValidateIssuer = !string.IsNullOrEmpty(authority),
                    ValidIssuer = authority,
                    
                    // Validate the token audience
                    ValidateAudience = !string.IsNullOrEmpty(audience),
                    ValidAudience = audience,
                    
                    // Validate token lifetime
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(60),
                    
                    // Require expiration claim
                    RequireExpirationTime = true,
                };

                // For test/local JWT validation: provide signing key directly
                if (!string.IsNullOrEmpty(jwtSecret))
                {
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
                    tokenValidationParams.IssuerSigningKey = key;
                    tokenValidationParams.ValidateIssuerSigningKey = true;
                    
                    // Also validate issuer and audience for test tokens
                    var issuer = authConfig["Jwt:Issuer"];
                    var testAudience = authConfig["Jwt:Audience"];
                    if (!string.IsNullOrEmpty(issuer))
                    {
                        tokenValidationParams.ValidateIssuer = true;
                        tokenValidationParams.ValidIssuer = issuer;
                    }
                    if (!string.IsNullOrEmpty(testAudience))
                    {
                        tokenValidationParams.ValidateAudience = true;
                        tokenValidationParams.ValidAudience = testAudience;
                    }
                }

                options.TokenValidationParameters = tokenValidationParams;

                // Optional: Configure event handlers for debugging/custom logic
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogInformation("Token validated for user: {User}", 
                            context.Principal?.Identity?.Name ?? "Unknown");
                        return Task.CompletedTask;
                    },

                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning(context.Exception, "Authentication failed");
                        
                        // Don't hide exceptions in development
                        if (context.HttpContext.RequestServices
                            .GetRequiredService<IHostEnvironment>().IsDevelopment())
                        {
                            context.NoResult();
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            context.Response.WriteAsJsonAsync(new
                            {
                                error = "Authentication failed",
                                message = context.Exception?.Message,
                                details = context.Exception?.ToString()
                            }).ConfigureAwait(false);
                        }
                        
                        return Task.CompletedTask;
                    },

                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning("Authentication challenge issued");
                        return Task.CompletedTask;
                    },

                    OnForbidden = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning("User forbidden - insufficient permissions");
                        return Task.CompletedTask;
                    }
                };

                // Configure secure token options
                options.SaveToken = true;
                options.IncludeErrorDetails = true;
            });

        return services;
    }

    /// <summary>
    /// Create a test JWT token for development (insecure - dev only).
    /// </summary>
    /// <remarks>
    /// This method is for development/testing only. Never use in production.
    /// </remarks>
    public static string CreateTestToken(
        string userId,
        string? email = null,
        string? name = null,
        params string[] roles)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new("oid", userId),
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userId),
        };

        if (!string.IsNullOrEmpty(email))
        {
            claims.Add(new("email", email));
            claims.Add(new(System.Security.Claims.ClaimTypes.Email, email));
        }

        if (!string.IsNullOrEmpty(name))
        {
            claims.Add(new("name", name));
            claims.Add(new(System.Security.Claims.ClaimTypes.Name, name));
        }

        foreach (var role in roles)
        {
            claims.Add(new("roles", role));
            claims.Add(new(System.Security.Claims.ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("dev-secret-key-256-bit-minimum-length-required"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "https://localhost",
            audience: "api://CloudWatcher",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }
}
