using System.Net.Http.Json;

namespace FoodStreet.Client.Services
{
    /// <summary>
    /// Service for handling authentication operations (login, register, logout)
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string email, string password);
        Task<AuthResult> RegisterAsync(string email, string password, string? fullName = null);
        Task LogoutAsync();
        Task<string?> GetTokenAsync();
        Task<bool> IsAuthenticatedAsync();
        event Action? OnAuthStateChanged;
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        
        private const string AccessTokenKey = "auth_access_token";
        private const string RefreshTokenKey = "auth_refresh_token";
        private const string TokenExpiryKey = "auth_token_expiry";
        private const string UserEmailKey = "auth_user_email";

        public event Action? OnAuthStateChanged;

        public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", new
                {
                    email,
                    password
                });

                var result = await response.Content.ReadFromJsonAsync<AuthApiResponse>();

                if (response.IsSuccessStatusCode && result?.Success == true)
                {
                    await StoreTokensAsync(result);
                    OnAuthStateChanged?.Invoke();
                    return AuthResult.Ok(result.Email);
                }

                return AuthResult.Fail(result?.Message ?? "Login failed", result?.Errors);
            }
            catch (Exception ex)
            {
                return AuthResult.Fail($"Connection error: {ex.Message}");
            }
        }

        public async Task<AuthResult> RegisterAsync(string email, string password, string? fullName = null)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/register", new
                {
                    email,
                    password,
                    fullName
                });

                var result = await response.Content.ReadFromJsonAsync<AuthApiResponse>();

                if (response.IsSuccessStatusCode && result?.Success == true)
                {
                    await StoreTokensAsync(result);
                    OnAuthStateChanged?.Invoke();
                    return AuthResult.Ok(result.Email);
                }

                return AuthResult.Fail(result?.Message ?? "Registration failed", result?.Errors);
            }
            catch (Exception ex)
            {
                return AuthResult.Fail($"Connection error: {ex.Message}");
            }
        }

        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync(AccessTokenKey);
            await _localStorage.RemoveItemAsync(RefreshTokenKey);
            await _localStorage.RemoveItemAsync(TokenExpiryKey);
            await _localStorage.RemoveItemAsync(UserEmailKey);
            OnAuthStateChanged?.Invoke();
        }

        public async Task<string?> GetTokenAsync()
        {
            var token = await _localStorage.GetItemAsync<string>(AccessTokenKey);
            var expiryString = await _localStorage.GetItemAsync<string>(TokenExpiryKey);

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expiryString))
                return null;

            if (DateTime.TryParse(expiryString, out var expiry) && expiry <= DateTime.UtcNow)
            {
                // Token expired
                await LogoutAsync();
                return null;
            }

            return token;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetTokenAsync();
            return !string.IsNullOrEmpty(token);
        }

        private async Task StoreTokensAsync(AuthApiResponse response)
        {
            if (!string.IsNullOrEmpty(response.AccessToken))
                await _localStorage.SetItemAsync(AccessTokenKey, response.AccessToken);

            if (!string.IsNullOrEmpty(response.RefreshToken))
                await _localStorage.SetItemAsync(RefreshTokenKey, response.RefreshToken);

            if (response.ExpiresAt.HasValue)
                await _localStorage.SetItemAsync(TokenExpiryKey, response.ExpiresAt.Value.ToString("O"));

            if (!string.IsNullOrEmpty(response.Email))
                await _localStorage.SetItemAsync(UserEmailKey, response.Email);
        }
    }

    // ========================================
    // DTOs
    // ========================================

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Email { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }

        public static AuthResult Ok(string? email = null) => new() { Success = true, Email = email };
        public static AuthResult Fail(string message, List<string>? errors = null) => new() 
        { 
            Success = false, 
            Message = message, 
            Errors = errors 
        };
    }

    public class AuthApiResponse
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? Email { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
    }
}
