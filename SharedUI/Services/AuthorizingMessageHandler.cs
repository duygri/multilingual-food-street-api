using System.Net.Http.Headers;

namespace FoodStreet.Client.Services
{
    /// <summary>
    /// HTTP message handler that automatically attaches JWT token to all requests
    /// </summary>
    public class AuthorizingMessageHandler : DelegatingHandler
    {
        private readonly ISessionStorageService _sessionStorage;

        // Keys must match AuthService constants
        private const string AccessTokenKey = "auth_access_token";

        public AuthorizingMessageHandler(ISessionStorageService sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            // Skip auth header for login/register endpoints
            var path = request.RequestUri?.PathAndQuery ?? "";
            if (!path.Contains("/api/content/auth/login") && !path.Contains("/api/content/auth/register"))
            {
                var token = await _sessionStorage.GetItemAsync<string>(AccessTokenKey);
                
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
