using System.Net.Http.Headers;

namespace FoodStreet.Client.Services
{
    /// <summary>
    /// HTTP message handler that automatically attaches JWT token to all requests
    /// </summary>
    public class AuthorizingMessageHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;

        // Keys must match AuthService constants
        private const string AccessTokenKey = "auth_access_token";

        public AuthorizingMessageHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            // Skip auth header for login/register endpoints
            var path = request.RequestUri?.PathAndQuery ?? "";
            if (!path.Contains("/api/auth/login") && !path.Contains("/api/auth/register"))
            {
                var token = await _localStorage.GetItemAsync<string>(AccessTokenKey);
                
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
