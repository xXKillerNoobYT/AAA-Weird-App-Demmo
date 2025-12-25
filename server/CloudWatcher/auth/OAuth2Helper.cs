using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

namespace CloudWatcher.Auth
{
    /// <summary>
    /// Helper class for OAuth2 token operations with Azure AD and Google Cloud
    /// </summary>
    public class OAuth2Helper
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tenantId;
        private readonly string _redirectUri;
        private readonly HttpClient _httpClient;

        public OAuth2Helper(string clientId, string clientSecret, string tenantId, string redirectUri)
        {
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
            _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
            _redirectUri = redirectUri ?? throw new ArgumentNullException(nameof(redirectUri));
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Get authorization URL for Azure AD login
        /// </summary>
        public string GetAuthorizationUrl(string state, string scope = "user.read offline_access")
        {
            return $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/authorize?" +
                   $"client_id={_clientId}&" +
                   $"response_type=code&" +
                   $"redirect_uri={Uri.EscapeDataString(_redirectUri)}&" +
                   $"scope={Uri.EscapeDataString(scope)}&" +
                   $"state={state}";
        }

        /// <summary>
        /// Exchange authorization code for access token
        /// </summary>
        public async Task<TokenResponse> GetTokenAsync(string authCode)
        {
            var tokenEndpoint = $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("code", authCode),
                new KeyValuePair<string, string>("redirect_uri", _redirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("scope", "user.read offline_access")
            });

            try
            {
                var response = await _httpClient.PostAsync(tokenEndpoint, content);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                return new TokenResponse
                {
                    AccessToken = tokenData.GetProperty("access_token").GetString(),
                    RefreshToken = tokenData.GetProperty("refresh_token").GetString(),
                    ExpiresIn = tokenData.GetProperty("expires_in").GetInt32(),
                    TokenType = tokenData.GetProperty("token_type").GetString(),
                    Success = true
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token exchange failed: {ex.Message}");
                return new TokenResponse { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
        {
            var tokenEndpoint = $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("scope", "user.read offline_access")
            });

            try
            {
                var response = await _httpClient.PostAsync(tokenEndpoint, content);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                return new TokenResponse
                {
                    AccessToken = tokenData.GetProperty("access_token").GetString(),
                    RefreshToken = tokenData.GetProperty("refresh_token").GetString(),
                    ExpiresIn = tokenData.GetProperty("expires_in").GetInt32(),
                    TokenType = tokenData.GetProperty("token_type").GetString(),
                    Success = true
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token refresh failed: {ex.Message}");
                return new TokenResponse { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// Validate token by calling Microsoft Graph
        /// </summary>
        public async Task<bool> ValidateTokenAsync(string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Response from OAuth2 token endpoint
    /// </summary>
    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}
