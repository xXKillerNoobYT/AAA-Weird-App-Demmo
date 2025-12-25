# CloudWatcher OAuth2 Authentication Setup Guide

## Overview

CloudWatcher implements **OAuth2 + JWT authentication** using Azure AD (Azure Active Directory) with support for local development scenarios. This guide covers configuration, troubleshooting, and best practices.

**Key Features:**
- Azure AD OAuth2 integration with OIDC (OpenID Connect)
- JWT bearer token validation
- Role-based access control (RBAC)
- Configurable authentication policies (allow unauthenticated for health checks)
- Multi-environment support (Dev/Staging/Production)

---

## Architecture

### Authentication Flow

```
┌─────────────┐         ┌──────────────┐         ┌─────────────────────┐
│   Client    │ ───────>│ CloudWatcher │ ───────>│   Azure AD / OIDC   │
│  (Mobile)   │ OAuth2  │   /api/v2/*  │ OIDC    │ (login.microsoftcl) │
└─────────────┘         │   Endpoint   │         └─────────────────────┘
                        └──────────────┘                    │
                               ▲                             │
                               │         Token Response      │
                               └─────────────────────────────┘

Token Flow:
1. Client requests OAuth2 token from Azure AD
2. Azure AD returns JWT token (with claims: oid, email, roles, etc.)
3. Client sends JWT in Authorization: Bearer <token> header
4. CloudWatcher validates JWT signature against Azure AD's public keys
5. If valid, request proceeds with user identity from JWT claims
6. If invalid/missing, request rejected with 401 Unauthorized
```

### Configuration Structure

**Key Configuration Sections (appsettings.json):**

```json
{
  "Authentication": {
    "Authority": "https://login.microsoftonline.com/common/v2.0",
    "Audience": "api://CloudWatcher",
    "ClientId": "your-azure-ad-client-id",
    "ClientSecret": "your-azure-ad-client-secret",
    "TenantId": "your-tenant-id",
    "Issuer": "https://login.microsoftonline.com/{tenantId}/v2.0",
    "AllowUnauthenticatedRequests": true,
    "EnableLocalJwtValidation": true,
    "TokenClaimMappings": {
      "UserId": "oid",
      "Email": "email",
      "Name": "name",
      "Roles": "roles"
    }
  },
  "HealthCheck": {
    "AuthServiceTimeoutSeconds": 5
  }
}
```

---

## Setup Instructions

### Prerequisites

- Azure AD tenant access (or local Azure emulation)
- .NET 10.0 SDK installed
- CloudWatcher project cloned

### Step 1: Register Application in Azure AD

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations** → **New registration**
3. **Name:** CloudWatcher (or CloudWatcher-Dev, CloudWatcher-Prod)
4. **Supported account types:** Choose based on your organization
5. **Redirect URI:** 
   - Web: `https://localhost:7001/api/v2/callback` (for token callback)
   - Web: `https://your-domain.com/api/v2/callback` (production)
6. Click **Register**

### Step 2: Configure Application

After registration, you'll see the **Application (client) ID**. Now configure:

#### 2a. Create Client Secret

1. Go to **Certificates & secrets**
2. Click **New client secret**
3. Description: "CloudWatcher-API-Secret"
4. Expires: 24 months (or per your security policy)
5. Copy the secret value (you won't see it again!)

#### 2b. Configure API Permissions

1. Go to **API permissions**
2. Click **Add a permission** → **Microsoft Graph** → **Delegated permissions**
3. Add: `openid`, `profile`, `email`, `User.Read`
4. Click **Grant admin consent**

#### 2c. Configure Token Claims

1. Go to **Token configuration**
2. Click **Add groups claim**
3. Select "Security groups" and "Application roles"
4. Optional: Add custom claims for your organization

### Step 3: Update appsettings Files

Copy values from Azure AD registration to your environment configs:

#### appsettings.Development.json
```json
{
  "Authentication": {
    "Authority": "https://login.microsoftonline.com/your-tenant-id/v2.0",
    "Audience": "api://cloudwatcher-dev",
    "ClientId": "00000000-0000-0000-0000-000000000000",  // From Step 1
    "ClientSecret": "your-secret-from-step-2a",
    "TenantId": "your-tenant-id",
    "AllowUnauthenticatedRequests": true,
    "EnableLocalJwtValidation": true
  },
  "HealthCheck": {
    "AuthServiceTimeoutSeconds": 10  // Lenient for debugging
  }
}
```

#### appsettings.Production.json
```json
{
  "Authentication": {
    "Authority": "https://login.microsoftonline.com/your-tenant-id/v2.0",
    "Audience": "api://CloudWatcher",
    "ClientId": "00000000-0000-0000-0000-000000000000",
    "ClientSecret": "your-production-secret",
    "TenantId": "your-tenant-id",
    "AllowUnauthenticatedRequests": false,
    "EnableLocalJwtValidation": false
  },
  "HealthCheck": {
    "AuthServiceTimeoutSeconds": 3  // Strict for performance
  }
}
```

### Step 4: Verify Configuration

Run health check endpoint:

```bash
# Start server
cd server/CloudWatcher
dotnet run

# In another terminal, test health endpoint
curl -X GET http://localhost:5000/api/v2/health

# Expected response:
{
  "Status": "healthy",
  "Database": "available",
  "Authentication": "available",
  "Timestamp": "2025-12-25T21:05:00Z",
  "Version": "1.0.0"
}
```

---

## Configuration Reference

### Environment-Specific Settings

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| **Authority** | common/v2.0 (multi-tenant) | {tenantId}/v2.0 (single tenant) | Token issuer endpoint |
| **Audience** | api://cloudwatcher-dev | api://CloudWatcher | Expected token audience |
| **AllowUnauthenticatedRequests** | true | false | Health checks + debugging |
| **EnableLocalJwtValidation** | true | false | Use local key cache vs. Azure |
| **AuthServiceTimeoutSeconds** | 10 | 3 | Health check timeout |

### Token Claim Mappings

CloudWatcher extracts these claims from JWT:

| JWT Claim | Mapping | Use |
|-----------|---------|-----|
| `oid` | UserId | User identification |
| `email` | Email | Contact & routing |
| `name` | Name | Display name |
| `roles` | Roles | Authorization decisions |

**Example JWT Payload:**
```json
{
  "oid": "00000000-0000-0000-0000-000000000000",
  "email": "user@company.com",
  "name": "John Doe",
  "roles": ["admin", "approver", "user"],
  "iat": 1703085900,
  "exp": 1703089500,
  "iss": "https://login.microsoftonline.com/your-tenant-id/v2.0",
  "aud": "api://CloudWatcher"
}
```

---

## API Integration

### Using Authenticated Endpoints

All endpoints except `/health` require `Authorization: Bearer <token>` header:

```bash
# Get orders (requires token)
curl -X GET http://localhost:5000/api/v2/orders \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."

# Health endpoint (no auth required)
curl -X GET http://localhost:5000/api/v2/health
```

### Handling Authentication Errors

**401 Unauthorized** - Missing or invalid token:
```json
{
  "error": "Unauthorized",
  "message": "Bearer token is missing or invalid"
}
```

**403 Forbidden** - Valid token but insufficient permissions:
```json
{
  "error": "Forbidden",
  "message": "User role 'user' not authorized for this endpoint"
}
```

---

## Health Check Integration

The `/health` endpoint includes authentication service status:

```json
{
  "Status": "healthy",
  "Authentication": "available",  // "available" | "unavailable" | "timeout" | "error"
  "Timestamp": "2025-12-25T21:05:00Z"
}
```

### Health Check Logic

1. **Pings** OAuth2 metadata endpoint: `{authority}/.well-known/openid-configuration`
2. **Timeout:** Configurable per environment (Dev=10s, Prod=3s)
3. **Non-critical:** Health endpoint succeeds even if auth service unavailable
4. **Graceful degradation:** Returns "unavailable" status without failing

---

## Troubleshooting

### Issue: "Authority not reachable"

**Symptom:** Health endpoint returns `"Authentication": "timeout"`

**Solutions:**
1. Verify Azure AD tenant ID in appsettings
2. Check network connectivity: `curl https://login.microsoftonline.com/.../.well-known/openid-configuration`
3. Increase timeout in appsettings: `HealthCheck:AuthServiceTimeoutSeconds`

### Issue: "Invalid token signature"

**Symptom:** 401 Unauthorized for all requests

**Solutions:**
1. Verify token is from correct Azure AD tenant
2. Check ClientId and Audience match registration
3. If using local validation, ensure key cache is refreshed (service restart)
4. Check token expiration: `jwt.io` decoder

### Issue: "Wrong audience"

**Symptom:** 401 Unauthorized, "Token audience mismatch"

**Solutions:**
1. Verify `Audience` in appsettings matches Azure AD app registration
2. Default is `api://CloudWatcher` (adjust if custom)
3. Request new token with correct audience scope

### Issue: "Missing claims"

**Symptom:** Roles/email appear as null despite valid token

**Solutions:**
1. Verify claims are included in Azure AD token response
2. Check `TokenClaimMappings` match actual JWT claims
3. May need to add claims to Azure AD app manifest

---

## Development vs. Production

### Development Settings
- **Multi-tenant authority** allows any Azure AD tenant
- **Unauthenticated requests allowed** for testing health checks
- **Generous timeouts** (10 seconds) for debugging
- **Local validation** uses cached keys

### Production Settings
- **Single-tenant authority** restricts to your organization
- **All requests require authentication** (except explicit allowlist)
- **Strict timeouts** (3 seconds) for performance
- **Remote validation** always fetches fresh keys from Azure

---

## Security Best Practices

1. **Never commit secrets** to version control
   - Use environment variables or Azure Key Vault
   - appsettings.json contains placeholders only

2. **Rotate client secrets** regularly
   - Azure AD: Set to 6-12 month expiration
   - Create new secret before old one expires

3. **Use HTTPS in production**
   - All token exchanges must be encrypted
   - Enforce in production config

4. **Limit token lifetime**
   - Access tokens: 1 hour typical
   - Refresh tokens: 24 hours to 7 days
   - Configure in Azure AD token policies

5. **Monitor authentication failures**
   - Log 401/403 responses
   - Alert on unusual patterns (potential attacks)

6. **Scope permissions appropriately**
   - Only request necessary API permissions
   - Regularly audit application permissions

---

## Local Development Without Azure AD

For local development without Azure AD access:

### Option 1: Mock Authentication Middleware
```csharp
// In Program.cs (development only)
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        // Inject fake user for testing
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, "dev-user-id"),
            new Claim("email", "dev@localhost"),
            new Claim(ClaimTypes.Role, "admin")
        };
        var identity = new ClaimsIdentity(claims, "test");
        context.User = new ClaimsPrincipal(identity);
        await next();
    });
}
```

### Option 2: Azure AD B2C (Emulated)
Use Azure AD B2C with local policies instead of production Azure AD.

---

## References

- [Microsoft Identity Platform Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [OAuth2 & OpenID Connect](https://oauth.net/2/)
- [JWT.io Token Decoder](https://jwt.io)
- [Azure AD Token Reference](https://docs.microsoft.com/en-us/azure/active-directory/develop/access-tokens)

---

## Next Steps

1. ✅ Register application in Azure AD
2. ✅ Obtain ClientId and ClientSecret
3. ✅ Update appsettings files
4. ✅ Test health endpoint
5. ✅ Test authenticated endpoint (requires mobile app or curl)
6. Monitor logs for authentication issues
7. Set up alerts for failed token validations

---

**Document Version:** 1.0  
**Last Updated:** 2025-12-25  
**Author:** CloudWatcher DevOps Team
