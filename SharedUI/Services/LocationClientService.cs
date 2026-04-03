using System.Net.Http.Json;
using FoodStreet.Client.DTOs;

namespace FoodStreet.Client.Services
{
    public interface ILocationClientService
    {
        // Public
        Task<PagedResult<LocationDto>?> GetNearestLocations(double lat, double lng, int page, int pageSize);
        Task<LocationDto?> GetLocation(int id);
        Task<List<LocationDto>> GetApprovedLocations();

        // Admin
        Task<List<LocationDto>> GetAllLocations();
        Task<List<LocationDto>> GetPendingLocations();
        Task ApproveLocation(int id);
        Task RejectLocation(int id);

        // POI Owner
        Task<List<LocationDto>> GetMyLocations();
        Task CreateLocation(LocationDto location);
        Task UpdateLocation(int id, LocationDto location);
        Task DeleteLocation(int id);
        
        // Mobile enhancements
        Task<List<LocationTranslationDto>> GetLocationTranslationsAsync(int locationId);
        Task<List<LocationDto>> GetSimilarLocationsAsync(int locationId);
        
        // Helper
        Task<string?> UploadImageAsync(MultipartFormDataContent content);
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
                $"api/maps/locations/near?lat={lat}&lng={lng}&page={page}&pageSize={pageSize}");
        }

        public async Task<LocationDto?> GetLocation(int id)
        {
            return await _http.GetFromJsonAsync<LocationDto>($"api/maps/locations/{id}");
        }

        public async Task<List<LocationDto>> GetApprovedLocations()
        {
            return await _http.GetFromJsonAsync<List<LocationDto>>("api/maps/locations/approved") ?? new();
        }

        // === Admin ===
        public async Task<List<LocationDto>> GetAllLocations()
        {
            return await _http.GetFromJsonAsync<List<LocationDto>>("api/maps/locations/admin/all") ?? new();
        }

        public async Task<List<LocationDto>> GetPendingLocations()
        {
            return await _http.GetFromJsonAsync<List<LocationDto>>("api/maps/locations/admin/pending") ?? new();
        }

        public async Task ApproveLocation(int id)
        {
            await _http.PostAsync($"api/maps/locations/admin/{id}/approve", null);
        }

        public async Task RejectLocation(int id)
        {
            await _http.PostAsync($"api/maps/locations/admin/{id}/reject", null);
        }

        // === POI Owner ===
        public async Task<List<LocationDto>> GetMyLocations()
        {
            return await _http.GetFromJsonAsync<List<LocationDto>>("api/maps/locations/my") ?? new();
        }

        public async Task CreateLocation(LocationDto location)
        {
            await _http.PostAsJsonAsync("api/maps/locations", location);
        }

        public async Task UpdateLocation(int id, LocationDto location)
        {
            await _http.PutAsJsonAsync($"api/maps/locations/{id}", location);
        }

        public async Task DeleteLocation(int id)
        {
            await _http.DeleteAsync($"api/maps/locations/{id}");
        }
        
        // === Mobile ===
        public async Task<List<LocationTranslationDto>> GetLocationTranslationsAsync(int locationId)
        {
            try
            {
                // Fetch content for each supported language
                var languages = new[] { "vi-VN", "en-US", "ja-JP", "ko-KR", "zh-CN" };
                var translations = new List<LocationTranslationDto>();
                
                foreach (var lang in languages)
                {
                    try
                    {
                        var result = await _http.GetFromJsonAsync<LocationTranslationDto>(
                            $"api/localization/location/{locationId}?lang={lang}");
                        if (result != null && !result.FallbackUsed)
                        {
                            translations.Add(result);
                        }
                    }
                    catch { /* Skip unavailable language */ }
                }
                
                return translations;
            }
            catch
            {
                return new();
            }
        }

        public async Task<List<LocationDto>> GetSimilarLocationsAsync(int locationId)
        {
            try
            {
                // Get current location to find its category
                var current = await _http.GetFromJsonAsync<LocationDto>($"api/maps/locations/{locationId}");
                if (current?.CategoryId == null) return new();
                
                // Get all approved, filter by same category, exclude self
                var allApproved = await _http.GetFromJsonAsync<List<LocationDto>>("api/maps/locations/approved") ?? new();
                return allApproved
                    .Where(l => l.CategoryId == current.CategoryId && l.Id != locationId)
                    .Take(5)
                    .ToList();
            }
            catch
            {
                return new();
            }
        }
        
        // === Helper ===
        public async Task<string?> UploadImageAsync(MultipartFormDataContent content)
        {
            var response = await _http.PostAsync("api/admin/image/upload", content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UploadImageResult>();
                return result?.Url;
            }
            return null;
        }
        
        private class UploadImageResult
        {
            public string Url { get; set; } = "";
        }
    }
}
