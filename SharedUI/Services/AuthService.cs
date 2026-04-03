using System.Net.Http.Json;

namespace FoodStreet.Client.Services
{
    /// <summary>
    /// Service for handling authentication operations (login, register, logout)
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string email, string password);
        Task<AuthResult> RegisterAsync(string email, string password, string? fullName = null, string role = "POI Owner");
        Task LogoutAsync();
        Task ClearTokensAsync();
        Task<string?> GetTokenAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<AuthResult> ChangePasswordAsync(string currentPassword, string newPassword);
        Task<AuthResult> UpdateProfileAsync(string displayName);
        void NotifyStateChanged();
        event Action? OnAuthStateChanged;
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ISessionStorageService _sessionStorage;
        
        private const string AccessTokenKey = "auth_access_token";
        private const string TokenExpiryKey = "auth_token_expiry";
        private const string UserEmailKey = "auth_user_email";

        public event Action? OnAuthStateChanged;

        public AuthService(HttpClient httpClient, ISessionStorageService sessionStorage)
        {
            _httpClient = httpClient;
            _sessionStorage = sessionStorage;
        }

        private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/content/auth/login", new
                {
                    email,
                    password
                });

                var responseString = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(responseString))
                {
                     return AuthResult.Fail($"Server không phản hồi dữ liệu (Mã lỗi: {response.StatusCode})");
                }

                AuthApiResponse? result;
                try 
                {
                    result = System.Text.Json.JsonSerializer.Deserialize<AuthApiResponse>(responseString, _jsonOptions);
                }
                catch (System.Text.Json.JsonException)
                {
                     // Return HTML or plain text error for diagnosis
                     string excerpt = responseString.Length > 80 ? responseString.Substring(0, 80) + "..." : responseString;
                     return AuthResult.Fail($"Server lỗi rỗng/HTML (Mã: {response.StatusCode}): {excerpt}");
                }

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

        public async Task<AuthResult> RegisterAsync(string email, string password, string? fullName = null, string role = "POI Owner")
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/content/auth/register", new
                {
                    email,
                    password,
                    fullName,
                    role
                });

                var responseString = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(responseString))
                {
                     return AuthResult.Fail($"Server không phản hồi dữ liệu (Mã lỗi: {response.StatusCode})");
                }

                AuthApiResponse? result;
                try 
                {
                    result = System.Text.Json.JsonSerializer.Deserialize<AuthApiResponse>(responseString, _jsonOptions);
                }
                catch (System.Text.Json.JsonException)
                {
                     // Return HTML or plain text error for diagnosis
                     string excerpt = responseString.Length > 80 ? responseString.Substring(0, 80) + "..." : responseString;
                     return AuthResult.Fail($"Server lỗi rỗng/HTML (Mã: {response.StatusCode}): {excerpt}");
                }

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

        public async Task<AuthResult> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/content/auth/change-password", new
                {
                    currentPassword,
                    newPassword
                });
                var body = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ChangePasswordResponse>(body, _jsonOptions);
                if (response.IsSuccessStatusCode && result?.Success == true)
                    return AuthResult.Ok();
                return AuthResult.Fail(result?.Message ?? "Đổi mật khẩu thất bại");
            }
            catch (Exception ex)
            {
                return AuthResult.Fail($"Lỗi kết nối: {ex.Message}");
            }
        }

        public async Task<AuthResult> UpdateProfileAsync(string displayName)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/content/auth/update-profile", new
                {
                    displayName
                });
                var body = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ChangePasswordResponse>(body, _jsonOptions);
                if (response.IsSuccessStatusCode && result?.Success == true)
                    return AuthResult.Ok();
                return AuthResult.Fail(result?.Message ?? "Cập nhật thất bại");
            }
            catch (Exception ex)
            {
                return AuthResult.Fail($"Lỗi kết nối: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears stored tokens WITHOUT triggering OnAuthStateChanged.
        /// Use when you need to clear auth but don't want component tree re-renders.
        /// </summary>
        public async Task ClearTokensAsync()
        {
            await _sessionStorage.RemoveItemAsync(AccessTokenKey);
            await _sessionStorage.RemoveItemAsync("auth_refresh_token");
            await _sessionStorage.RemoveItemAsync(TokenExpiryKey);
            await _sessionStorage.RemoveItemAsync(UserEmailKey);
        }

        public async Task<string?> GetTokenAsync()
        {
            var token = await _sessionStorage.GetItemAsync<string>(AccessTokenKey);
            var expiryString = await _sessionStorage.GetItemAsync<string>(TokenExpiryKey);

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
                await _sessionStorage.SetItemAsync(AccessTokenKey, response.AccessToken);

            if (response.ExpiresAt.HasValue)
                await _sessionStorage.SetItemAsync(TokenExpiryKey, response.ExpiresAt.Value.ToString("O"));

            if (!string.IsNullOrEmpty(response.Email))
                await _sessionStorage.SetItemAsync(UserEmailKey, response.Email);
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
        public DateTime? ExpiresAt { get; set; }
        public string? Email { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
    }

    public class ChangePasswordResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
    }
}
