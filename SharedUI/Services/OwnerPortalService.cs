using System.Net.Http.Json;
using FoodStreet.Client.DTOs;

namespace FoodStreet.Client.Services
{
    public interface IOwnerPortalService
    {
        Task<OwnerDashboardDto?> GetDashboardAsync();
        Task<OwnerProfileDto?> GetProfileAsync();
        Task<bool> UpdateProfileAsync(UpdateOwnerProfileDto request);
        Task<OwnerAnalyticsDto?> GetAnalyticsAsync(int days = 30, int? locationId = null);
    }

    public class OwnerPortalService : IOwnerPortalService
    {
        private readonly HttpClient _http;

        public OwnerPortalService(HttpClient http)
        {
            _http = http;
        }

        public async Task<OwnerDashboardDto?> GetDashboardAsync()
        {
            return await _http.GetFromJsonAsync<OwnerDashboardDto>("api/owner/dashboard");
        }

        public async Task<OwnerProfileDto?> GetProfileAsync()
        {
            return await _http.GetFromJsonAsync<OwnerProfileDto>("api/owner/profile");
        }

        public async Task<bool> UpdateProfileAsync(UpdateOwnerProfileDto request)
        {
            var response = await _http.PutAsJsonAsync("api/owner/profile", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<OwnerAnalyticsDto?> GetAnalyticsAsync(int days = 30, int? locationId = null)
        {
            var query = locationId.HasValue && locationId.Value > 0
                ? $"api/owner/analytics?days={days}&locationId={locationId.Value}"
                : $"api/owner/analytics?days={days}";
            return await _http.GetFromJsonAsync<OwnerAnalyticsDto>(query);
        }
    }
}
