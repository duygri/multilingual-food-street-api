using System.Net.Http.Json;
using FoodStreet.Client.DTOs;

namespace FoodStreet.Client.Services
{
    public interface IFoodClientService
    {
        Task<List<FoodDto>> GetAllFoods();
        Task<List<FoodDto>> GetFoodsByLocation(int locationId);
        Task<FoodDto?> GetFood(int id);
        Task CreateFood(FoodDto food);
        Task UpdateFood(int id, FoodDto food);
        Task DeleteFood(int id);
        Task UploadAudio(int foodId, MultipartFormDataContent content);
        event Action OnLanguageChanged;
        void SetLanguage(string languageCode);
        string CurrentLanguage { get; }
    }

    public class FoodClientService : IFoodClientService
    {
        private readonly HttpClient _http;
        public event Action? OnLanguageChanged;
        public string CurrentLanguage { get; private set; } = "vi-VN";

        public FoodClientService(HttpClient http)
        {
            _http = http;
        }

        public void SetLanguage(string languageCode)
        {
            CurrentLanguage = languageCode;
            var culture = new System.Globalization.CultureInfo(languageCode);
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

            _http.DefaultRequestHeaders.AcceptLanguage.Clear();
            _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd(languageCode);
            OnLanguageChanged?.Invoke();
        }

        public async Task<List<FoodDto>> GetAllFoods()
        {
            return await _http.GetFromJsonAsync<List<FoodDto>>("api/Food") ?? new();
        }

        public async Task<List<FoodDto>> GetFoodsByLocation(int locationId)
        {
            return await _http.GetFromJsonAsync<List<FoodDto>>($"api/Food/by-location/{locationId}") ?? new();
        }

        public async Task<FoodDto?> GetFood(int id)
        {
            return await _http.GetFromJsonAsync<FoodDto>($"api/Food/{id}");
        }

        public async Task CreateFood(FoodDto food)
        {
            await _http.PostAsJsonAsync("api/Food", food);
        }

        public async Task UpdateFood(int id, FoodDto food)
        {
            await _http.PutAsJsonAsync($"api/Food/{id}", food);
        }

        public async Task DeleteFood(int id)
        {
            await _http.DeleteAsync($"api/Food/{id}");
        }

        public async Task UploadAudio(int foodId, MultipartFormDataContent content)
        {
            content.Add(new StringContent(foodId.ToString()), "foodId");
            await _http.PostAsync("api/admin/audio/upload", content);
        }
    }
}
