using System.Net.Http.Json;
using FoodStreet.Client.DTOs;

namespace FoodStreet.Client.Services
{
    public interface ITourClientService
    {
        Task<List<TourDto>> GetActiveToursAsync();
        Task<TourDto?> GetTourAsync(int id);
        Task<TourResumeSnapshotDto?> GetLatestResumeAsync();
        Task<TourSessionDto?> GetSessionAsync(int id, string sessionId);
        Task<TourSessionDto?> ResumeTourAsync(int id, ResumeTourRequestDto request);
        Task<TourSessionDto?> StartTourAsync(int id, StartTourRequestDto? request = null);
        Task<TourSessionDto?> UpdateProgressAsync(int id, TourProgressRequestDto request);
        Task DismissSessionAsync(string sessionId);
    }

    public class TourClientService : ITourClientService
    {
        private readonly HttpClient _http;

        public TourClientService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<TourDto>> GetActiveToursAsync()
        {
            return await _http.GetFromJsonAsync<List<TourDto>>("api/tours?activeOnly=true") ?? new();
        }

        public async Task<TourDto?> GetTourAsync(int id)
        {
            return await _http.GetFromJsonAsync<TourDto>($"api/tours/{id}");
        }

        public async Task<TourResumeSnapshotDto?> GetLatestResumeAsync()
        {
            var response = await _http.GetAsync("api/tours/resume/latest");
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent || !response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TourResumeSnapshotDto>();
        }

        public async Task<TourSessionDto?> GetSessionAsync(int id, string sessionId)
        {
            var response = await _http.GetAsync($"api/tours/{id}/sessions/{Uri.EscapeDataString(sessionId)}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TourSessionDto>();
        }

        public async Task<TourSessionDto?> ResumeTourAsync(int id, ResumeTourRequestDto request)
        {
            var response = await _http.PostAsJsonAsync($"api/tours/{id}/resume", request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TourSessionDto>();
        }

        public async Task<TourSessionDto?> StartTourAsync(int id, StartTourRequestDto? request = null)
        {
            var response = await _http.PostAsJsonAsync($"api/tours/{id}/start", request ?? new StartTourRequestDto());
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TourSessionDto>();
        }

        public async Task<TourSessionDto?> UpdateProgressAsync(int id, TourProgressRequestDto request)
        {
            var response = await _http.PostAsJsonAsync($"api/tours/{id}/progress", request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TourSessionDto>();
        }

        public async Task DismissSessionAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            await _http.DeleteAsync($"api/tours/sessions/{Uri.EscapeDataString(sessionId)}");
        }
    }
}
