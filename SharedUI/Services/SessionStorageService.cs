using Microsoft.JSInterop;

namespace FoodStreet.Client.Services
{
    /// <summary>
    /// Simple SessionStorage service for Blazor WASM
    /// Uses JavaScript interop to access browser's sessionStorage
    /// Data is cleared when the page session ends (e.g. tab closed or duplicated)
    /// </summary>
    public interface ISessionStorageService
    {
        Task<T?> GetItemAsync<T>(string key);
        Task SetItemAsync<T>(string key, T value);
        Task RemoveItemAsync(string key);
    }

    public class SessionStorageService : ISessionStorageService
    {
        private readonly IJSRuntime _jsRuntime;

        public SessionStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<T?> GetItemAsync<T>(string key)
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", key);
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

                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", key, json);
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
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", key);
            }
            catch
            {
                // Ignore storage errors
            }
        }
    }
}
