using System.Net.Http.Json;

namespace FoodStreet.Client.Services
{
    public interface IUserService
    {
        Task<List<UserDto>> GetUsersAsync();
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
}
