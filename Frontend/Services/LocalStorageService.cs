using Microsoft.JSInterop;

namespace FoodStreet.Client.Services
{
    /// <summary>
    /// Simple LocalStorage service for Blazor WASM
    /// Uses JavaScript interop to access browser's localStorage
    /// </summary>
    public interface ILocalStorageService
    {
        Task<T?> GetItemAsync<T>(string key);
        Task SetItemAsync<T>(string key, T value);
        Task RemoveItemAsync(string key);
    }

    public class LocalStorageService : ILocalStorageService
    {
        private readonly IJSRuntime _jsRuntime;

        public LocalStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<T?> GetItemAsync<T>(string key)
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
                if (string.IsNullOrEmpty(json))
                    return default;

                if (typeof(T) == typeof(string))
                    return (T)(object)json;

                return System.Text.Json.JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return default;
            }
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            try
            {
                string json;
                if (typeof(T) == typeof(string))
                    json = value?.ToString() ?? string.Empty;
                else
                    json = System.Text.Json.JsonSerializer.Serialize(value);

                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
            }
            catch
            {
                // Ignore storage errors
            }
        }

        public async Task RemoveItemAsync(string key)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch
            {
                // Ignore storage errors
            }
        }
    }
}
