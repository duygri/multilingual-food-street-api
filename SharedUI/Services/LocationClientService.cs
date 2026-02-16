using System.Net.Http.Json;
using FoodStreet.Client.DTOs;

namespace FoodStreet.Client.Services
{
    public interface ILocationClientService
    {
        // Public
        Task<PagedResult<LocationDto>?> GetNearestLocations(double lat, double lng, int page, int pageSize);
        Task<LocationDto?> GetLocation(int id);

        // Admin
        Task<List<LocationDto>> GetAllLocations();
        Task<List<LocationDto>> GetPendingLocations();
        Task ApproveLocation(int id);
        Task RejectLocation(int id);

        // Seller
        Task<List<LocationDto>> GetMyLocations();
        Task CreateLocation(LocationDto location);
        Task UpdateLocation(int id, LocationDto location);
        Task DeleteLocation(int id);
    }

    public class LocationClientService : ILocationClientService
    {
        private readonly HttpClient _http;

        public LocationClientService(HttpClient http)
        {
            _http = http;
        }

        // === Public ===
        public async Task<PagedResult<LocationDto>?> GetNearestLocations(double lat, double lng, int page, int pageSize)
        {
            return await _http.GetFromJsonAsync<PagedResult<LocationDto>>(
                $"api/location/near?lat={lat}&lng={lng}&page={page}&pageSize={pageSize}");
        }

        public async Task<LocationDto?> GetLocation(int id)
        {
            return await _http.GetFromJsonAsync<LocationDto>($"api/location/{id}");
        }

        // === Admin ===
        public async Task<List<LocationDto>> GetAllLocations()
        {
            return await _http.GetFromJsonAsync<List<LocationDto>>("api/location/admin/all") ?? new();
        }

        public async Task<List<LocationDto>> GetPendingLocations()
        {
            return await _http.GetFromJsonAsync<List<LocationDto>>("api/location/admin/pending") ?? new();
        }

        public async Task ApproveLocation(int id)
        {
            await _http.PostAsync($"api/location/admin/{id}/approve", null);
        }

        public async Task RejectLocation(int id)
        {
            await _http.PostAsync($"api/location/admin/{id}/reject", null);
        }

        // === Seller ===
        public async Task<List<LocationDto>> GetMyLocations()
        {
            return await _http.GetFromJsonAsync<List<LocationDto>>("api/location/my") ?? new();
        }

        public async Task CreateLocation(LocationDto location)
        {
            await _http.PostAsJsonAsync("api/location", location);
        }

        public async Task UpdateLocation(int id, LocationDto location)
        {
            await _http.PutAsJsonAsync($"api/location/{id}", location);
        }

        public async Task DeleteLocation(int id)
        {
            await _http.DeleteAsync($"api/location/{id}");
        }
    }
}
