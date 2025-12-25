using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CloudWatcher.Auth;

/// <summary>
/// Defines authorization policies for role-based access control.
/// 
/// Policies:
/// - AdminOnly: Requires admin role
/// - DeptManager: Requires dept manager or admin role
/// - BasicUser: Any authenticated user
/// </summary>
public static class AuthorizationPolicies
{
    public const string AdminOnlyPolicy = "AdminOnly";
    public const string DeptManagerPolicy = "DeptManager";
    public const string BasicUserPolicy = "BasicUser";

    /// <summary>
    /// Configure all authorization policies for the application.
    /// </summary>
    public static void AddCustomAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            // Admin policy - only users with admin role
            .AddPolicy(AdminOnlyPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("admin", "Admin", "ADMIN");
            })
            
            // DeptManager policy - users with manager or admin role
            .AddPolicy(DeptManagerPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("manager", "Manager", "admin", "Admin", "ADMIN");
            })
            
            // BasicUser policy - any authenticated user
            .AddPolicy(BasicUserPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
            });
    }
}

/// <summary>
/// Helper methods for checking user roles and claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extract the user ID from JWT claims.
    /// </summary>
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst("oid")?.Value 
            ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Extract the user email from JWT claims.
    /// </summary>
    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst("email")?.Value 
            ?? principal?.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Extract the user name from JWT claims.
    /// </summary>
    public static string? GetName(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst("name")?.Value 
            ?? principal?.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Check if user has any of the specified roles.
    /// </summary>
    public static bool HasRole(this ClaimsPrincipal principal, params string[] roles)
    {
        if (principal == null) return false;
        
        return roles.Any(role => 
            principal.HasClaim(ClaimTypes.Role, role) ||
            principal.HasClaim("roles", role));
    }

    /// <summary>
    /// Check if user is admin.
    /// </summary>
    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal?.HasRole("admin", "Admin", "ADMIN") ?? false;
    }

    /// <summary>
    /// Check if user is department manager.
    /// </summary>
    public static bool IsManager(this ClaimsPrincipal principal)
    {
        return principal?.HasRole("manager", "Manager", "admin", "Admin", "ADMIN") ?? false;
    }
}
