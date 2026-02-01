using System.Net.Http.Json;
using FoodStreet.Client.DTOs;

namespace FoodStreet.Client.Services
{
    public interface IAudioService
    {
        Task<List<AudioFileDto>> GetAllAudioAsync(int? foodId = null);
        Task<AudioFileDto?> GetAudioAsync(int id);
        Task<AudioStatsDto?> GetStatsAsync();
        Task<bool> DeleteAudioAsync(int id);
        Task<bool> AssignToFoodAsync(int audioId, int? foodId);
    }

    public class AudioService : IAudioService
    {
        private readonly HttpClient _http;

        public AudioService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<AudioFileDto>> GetAllAudioAsync(int? foodId = null)
        {
            try
            {
                var url = foodId.HasValue ? $"api/audio?foodId={foodId}" : "api/audio";
                return await _http.GetFromJsonAsync<List<AudioFileDto>>(url) ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<AudioFileDto?> GetAudioAsync(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<AudioFileDto>($"api/audio/{id}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<AudioStatsDto?> GetStatsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<AudioStatsDto>("api/audio/stats");
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteAudioAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/audio/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AssignToFoodAsync(int audioId, int? foodId)
        {
            try
            {
                var url = foodId.HasValue ? $"api/audio/{audioId}/assign?foodId={foodId}" : $"api/audio/{audioId}/assign";
                var response = await _http.PutAsync(url, null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
