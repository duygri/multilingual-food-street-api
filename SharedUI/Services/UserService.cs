using System.Net.Http.Json;

namespace FoodStreet.Client.Services
{
    public interface IUserService
    {
        Task<List<UserDto>> GetUsersAsync();
        Task<CreateUserResult> CreateUserAsync(string email, string password, string role);
        Task<bool> ApproveSellerAsync(string userId);
        Task<bool> ToggleLockAsync(string userId);
        Task<bool> DeleteUserAsync(string userId);
    }

    public class UserService : IUserService
    {
        private readonly HttpClient _http;

        public UserService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<UserDto>> GetUsersAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<UserDto>>("api/user") ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<CreateUserResult> CreateUserAsync(string email, string password, string role)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/user/create", new { email, password, role });
                if (response.IsSuccessStatusCode)
                {
                    return new CreateUserResult { Success = true };
                }
                var error = await response.Content.ReadFromJsonAsync<CreateUserErrorResponse>();
                return new CreateUserResult
                {
                    Success = false,
                    Message = error?.Message ?? "Tạo tài khoản thất bại",
                    Errors = error?.Errors
                };
            }
            catch (Exception ex)
            {
                return new CreateUserResult { Success = false, Message = $"Lỗi kết nối: {ex.Message}" };
            }
        }

        public async Task<bool> ApproveSellerAsync(string userId)
        {
            var response = await _http.PostAsync($"api/user/{userId}/approve", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ToggleLockAsync(string userId)
        {
            var response = await _http.PostAsync($"api/user/{userId}/toggle-lock", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var response = await _http.DeleteAsync($"api/user/{userId}");
            return response.IsSuccessStatusCode;
        }
    }

    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool IsLocked { get; set; }
    }

    public class CreateUserResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
    }

    public class CreateUserErrorResponse
    {
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
    }
}

