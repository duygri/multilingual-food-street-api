using System.Net.Http.Json;

namespace FoodStreet.Client.Services
{
    /// <summary>
    /// Service for handling authentication operations (login, register, logout)
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string email, string password);
        Task<AuthResult> RegisterAsync(string email, string password, string? fullName = null, string role = "Seller");
        Task LogoutAsync();
        Task ClearTokensAsync();
        Task<string?> GetTokenAsync();
        Task<bool> IsAuthenticatedAsync();
        void NotifyStateChanged();
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

        private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", new
                {
                    email,
                    password
                });

                var result = await response.Content.ReadFromJsonAsync<AuthApiResponse>(_jsonOptions);

                if (response.IsSuccessStatusCode && result?.Success == true)
                {
                    await StoreTokensAsync(result);
                    // NOTE: Do NOT call OnAuthStateChanged here.
                    // The caller (Login page) must call NotifyStateChanged() 
                    // AFTER verifying the user's role to prevent premature re-renders.
                    return AuthResult.Ok(result.Email);
                }

                return AuthResult.Fail(result?.Message ?? "Đăng nhập thất bại", result?.Errors);
            }
            catch (Exception ex)
            {
                return AuthResult.Fail($"Lỗi kết nối: {ex.Message}");
            }
        }

        public async Task<AuthResult> RegisterAsync(string email, string password, string? fullName = null, string role = "Seller")
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/register", new
                {
                    email,
                    password,
                    fullName,
                    role
                });

                var result = await response.Content.ReadFromJsonAsync<AuthApiResponse>(_jsonOptions);

                if (response.IsSuccessStatusCode && result?.Success == true)
                {
                    // Only store tokens if provided (Admin might approve later)
                    if (!string.IsNullOrEmpty(result.AccessToken))
                    {
                        await StoreTokensAsync(result);
                        OnAuthStateChanged?.Invoke();
                    }
                    return AuthResult.Ok(result.Email); // Or handle specific success message
                }

                return AuthResult.Fail(result?.Message ?? "Đăng ký thất bại", result?.Errors);
            }
            catch (Exception ex)
            {
                return AuthResult.Fail($"Lỗi kết nối: {ex.Message}");
            }
        }

        public void NotifyStateChanged()
        {
            OnAuthStateChanged?.Invoke();
        }

        public async Task LogoutAsync()
        {
            await ClearTokensAsync();
            OnAuthStateChanged?.Invoke();
        }

        /// <summary>
        /// Clears stored tokens WITHOUT triggering OnAuthStateChanged.
        /// Use when you need to clear auth but don't want component tree re-renders.
        /// </summary>
        public async Task ClearTokensAsync()
        {
            await _localStorage.RemoveItemAsync(AccessTokenKey);
            await _localStorage.RemoveItemAsync(RefreshTokenKey);
            await _localStorage.RemoveItemAsync(TokenExpiryKey);
            await _localStorage.RemoveItemAsync(UserEmailKey);
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
