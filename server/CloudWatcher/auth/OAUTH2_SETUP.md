# OAuth2 Setup Guide for WeirdToo Parts System

## Azure AD Configuration

### Step 1: Register Application in Azure AD

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to: **Azure Active Directory** → **App registrations** → **New registration**
3. Fill in the form:
   - **Name:** WeirdToo Parts System
   - **Supported account types:** Multi-tenant (allows both personal and work accounts)
   - **Redirect URI (optional):**
     - Web: `http://localhost:5000/auth/callback`

4. Click **Register**

### Step 2: Configure Platform Settings

1. Go to **Authentication** (left sidebar)
2. Under **Platform configurations**, click **Add a platform**
3. Select **Web**:
   - **Redirect URIs:**
     - `http://localhost:5000/auth/callback`
     - `http://localhost:5000/auth/google/callback` (for OAuth flow)

4. Under **Advanced settings**:
   - **Allow public client flows:** NO
   - **ID tokens:** Check this box (needed for auth flow)
   - **Access tokens:** Check this box

5. Click **Save**

### Step 3: Create Client Secret

1. Go to **Certificates & secrets** (left sidebar)
2. Click **New client secret**
3. Set description: `WeirdToo Desktop/Mobile Auth`
4. Set expiration: `12 months` (adjust as needed)
5. Click **Add**
6. **IMPORTANT:** Copy the secret value immediately (you won't see it again!)
7. Store in `appsettings.json` under `AzureAd.ClientSecret`

### Step 4: Configure API Permissions

1. Go to **API permissions** (left sidebar)
2. Click **Add a permission**
3. Select **Microsoft Graph**
4. Select **Delegated permissions**
5. Search for and select:
   - `User.Read` (basic profile access)
   - `offline_access` (refresh token)
6. Click **Add permissions**
7. Back at API permissions page, click **Grant admin consent for [your org]**

### Step 5: Collect Configuration Values

From the **Overview** tab, copy:

- **Directory (tenant) ID** → `AzureAd.TenantId` in appsettings.json
- **Application (client) ID** → `AzureAd.ClientId` in appsettings.json

From **Certificates & secrets**, you already have:

- **Client secret value** → `AzureAd.ClientSecret`

### Example appsettings.json Configuration

```json
{
  "AzureAd": {
    "TenantId": "12345678-1234-1234-1234-123456789012",
    "ClientId": "87654321-4321-4321-4321-210987654321",
    "ClientSecret": "YOUR_SECRET_VALUE_HERE~PASSWORD",
    "Authority": "https://login.microsoftonline.com/12345678-1234-1234-1234-123456789012/v2.0",
    "RedirectUri": "http://localhost:5000/auth/callback",
    "MobileRedirectUri": "msauth.com.weirdtoo.parts://auth"
  }
}
```

## Google Cloud Configuration (Optional - for Drive Support)

### Step 1: Create GCP Project

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Click **Select a Project** → **New Project**
3. Name: `WeirdToo Parts System`
4. Click **Create**

### Step 2: Enable Google Drive API

1. Search for **Google Drive API**
2. Click **Enable**
3. Go to **Credentials** (left sidebar)
4. Click **Create Credentials** → **OAuth client ID**
5. Choose **Desktop application**
6. Download the JSON file
7. Extract `client_id` and `client_secret` to `appsettings.json`

## Testing OAuth2 Flow

See `OAuth2Tests.cs` for automated token generation tests.

**Manual Test:**

```bash
dotnet run --project CloudWatcher
# Navigate to: http://localhost:5000/auth/login
# You should be redirected to Azure AD login
# After login, you should see your user profile
```

## Security Considerations

- **Never commit secrets** to version control (use environment variables or key vault)
- **Rotate secrets** every 90 days
- **Use PKCE** for mobile clients to prevent token interception
- **Set token expiry** to 1 hour; refresh tokens auto-rotate
- **Monitor token usage** in Azure AD audit logs

## Troubleshooting

| Issue                          | Solution                                                      |
| ------------------------------ | ------------------------------------------------------------- |
| "invalid_scope" error          | Ensure API permissions are granted and admin consented        |
| Redirect URI mismatch           | Check exact URL match in Azure AD config vs app config        |
| Token expired                   | Implement automatic refresh token flow                        |
| Mobile app not logging in       | Ensure mobile redirect URI is registered in Azure AD          |

## References

- [Microsoft Identity Platform](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [OAuth 2.0 Authorization Code Flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow)
- [PKCE for Native Apps](https://tools.ietf.org/html/rfc7636)
