# OAuth2 Quick Reference Card

## Configuration Checklist

### Azure AD Setup (5 minutes)
- [ ] Register app in Azure Portal
- [ ] Copy Application (client) ID
- [ ] Create client secret
- [ ] Add API permissions (openid, profile, email, User.Read)
- [ ] Grant admin consent

### CloudWatcher Configuration (2 minutes)
- [ ] Update appsettings.Development.json with ClientId, ClientSecret, TenantId
- [ ] Update appsettings.Production.json for production Azure AD tenant
- [ ] Set AllowUnauthenticatedRequests = true (dev) / false (prod)
- [ ] Verify HealthCheck:AuthServiceTimeoutSeconds (10s dev, 3s prod)

---

## Endpoints Summary

| Endpoint | Auth Required | Purpose |
|----------|---------------|---------|
| GET /api/v2/health | No | Server & auth health status |
| GET /api/v2/orders | Yes | List orders (requires Bearer token) |
| POST /api/v2/orders | Yes | Create order |
| PATCH /api/v2/inventory/{partId} | Yes | Update inventory with audit |

---

## Token Testing

### Using curl
```bash
# Get token from Azure AD (requires client credentials)
curl -X POST https://login.microsoftonline.com/TENANT-ID/oauth2/v2.0/token \
  -d "client_id=CLIENT-ID&client_secret=CLIENT-SECRET&scope=api://CloudWatcher/.default&grant_type=client_credentials"

# Use token in request
TOKEN=$(...)
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/v2/orders
```

### Using Postman
1. Create **OAuth2 Authorization Code** request
2. Set Token URL: `https://login.microsoftonline.com/TENANT-ID/oauth2/v2.0/token`
3. Client ID: From Azure AD
4. Client Secret: From Azure AD
5. Scope: `api://CloudWatcher/.default`

---

## Health Check Response

```json
{
  "Status": "healthy",
  "Database": "available",
  "Authentication": "available",
  "Timestamp": "2025-12-25T21:05:00Z",
  "Version": "1.0.0"
}
```

**Authentication values:**
- `"available"` - OAuth2 service responding
- `"unavailable"` - OAuth2 service not responding (health still OK)
- `"not-configured"` - Authority not set in config
- `"timeout"` - Auth service took too long
- `"error"` - Unexpected error

---

## Common Issues & Fixes

| Issue | Symptom | Fix |
|-------|---------|-----|
| Wrong tenant | 401 on valid token | Use correct TenantId in Authority |
| Missing secret | Cannot obtain token | Create secret in Azure AD |
| Wrong audience | "audience_mismatch" error | Verify Audience matches app registration |
| Timeout | "Authentication": "timeout" | Increase HealthCheck:AuthServiceTimeoutSeconds |
| Network blocked | Cannot reach login.microsoftonline.com | Check firewall/proxy settings |

---

## Environment Variables (Optional)

Instead of appsettings, use environment variables:

```bash
export AUTHENTICATION__CLIENTID="your-client-id"
export AUTHENTICATION__CLIENTSECRET="your-secret"
export AUTHENTICATION__TENANTID="your-tenant-id"
export HEALTHCHECK__AUTHSERVICETIMEOUTSECONDS="5"
```

---

## File Locations

- **Setup Guide:** `Docs/OAUTH2_SETUP_GUIDE.md`
- **Configuration:** `server/CloudWatcher/appsettings*.json`
- **Code:** `server/CloudWatcher/Auth/`
- **Health Endpoint:** `server/CloudWatcher/Controllers/HealthController.cs`

---

**Version:** 1.0 | **Updated:** 2025-12-25
