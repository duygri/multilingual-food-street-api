using System.Net.Http.Json;
using FoodStreet.Client.DTOs;

namespace FoodStreet.Client.Services
{
    public interface IAudioService
    {
        Task<List<AudioFileDto>> GetAllAudioAsync(int? locationId = null);
        Task<AudioFileDto?> GetAudioAsync(int id);
        Task<AudioStatsDto?> GetStatsAsync();
        Task<bool> DeleteAudioAsync(int id);
        Task<bool> AssignToLocationAsync(int audioId, int? locationId);
        Task<AudioFileDto?> UploadAudioAsync(HttpContent content, string fileName, Action<double> onProgress, int? locationId = null);
    }

    public class AudioService : IAudioService
    {
        private readonly HttpClient _http;

        public AudioService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<AudioFileDto>> GetAllAudioAsync(int? locationId = null)
        {
            try
            {
                var url = locationId.HasValue ? $"api/audio?locationId={locationId}" : "api/audio";
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

        public async Task<bool> AssignToLocationAsync(int audioId, int? locationId)
        {
            try
            {
                var url = locationId.HasValue 
                    ? $"api/audio/{audioId}/assign?locationId={locationId}" 
                    : $"api/audio/{audioId}/assign";
                var response = await _http.PutAsync(url, null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<AudioFileDto?> UploadAudioAsync(HttpContent content, string fileName, Action<double> onProgress, int? locationId = null)
        {
            try
            {
                var progressContent = new ProgressableStreamContent(content, (sent, total) =>
                {
                    if (total > 0)
                    {
                        var percentage = (double)sent / total * 100;
                        onProgress(percentage);
                    }
                });

                using var formData = new MultipartFormDataContent();
                formData.Add(progressContent, "file", fileName);

                var uploadUrl = locationId.HasValue 
                    ? $"api/audio?locationId={locationId}" 
                    : "api/audio";
                var response = await _http.PostAsync(uploadUrl, formData);
                
                if (response.IsSuccessStatusCode)
                {
                     return await response.Content.ReadFromJsonAsync<AudioFileDto>();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload failed: {ex.Message}");
                return null;
            }
        }
    }
}
