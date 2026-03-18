using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoodStreet.Client.DTOs;

namespace FoodStreet.Client.Services
{
    public interface IFavoritesService
    {
        Task<List<LocationDto>> GetFavoritesAsync();
        Task<bool> IsFavoriteAsync(int locationId);
        Task AddFavoriteAsync(LocationDto location);
        Task RemoveFavoriteAsync(int locationId);
        Task ToggleFavoriteAsync(LocationDto location);
    }

    /// <summary>
    /// Persists favorites in localStorage.
    /// Key: "fs_favorites" → JSON array of LocationDto (serialized by LocalStorageService).
    /// </summary>
    public class FavoritesService : IFavoritesService
    {
        private const string StorageKey = "fs_favorites";
        private readonly ILocalStorageService _localStorage;

        public FavoritesService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<List<LocationDto>> GetFavoritesAsync()
        {
            return await _localStorage.GetItemAsync<List<LocationDto>>(StorageKey) ?? new();
        }

        public async Task<bool> IsFavoriteAsync(int locationId)
        {
            var list = await GetFavoritesAsync();
            return list.Any(l => l.Id == locationId);
        }

        public async Task AddFavoriteAsync(LocationDto location)
        {
            var list = await GetFavoritesAsync();
            if (list.All(l => l.Id != location.Id))
            {
                list.Add(location);
                await _localStorage.SetItemAsync(StorageKey, list);
            }
        }

        public async Task RemoveFavoriteAsync(int locationId)
        {
            var list = await GetFavoritesAsync();
            list.RemoveAll(l => l.Id == locationId);
            await _localStorage.SetItemAsync(StorageKey, list);
        }

        public async Task ToggleFavoriteAsync(LocationDto location)
        {
            if (await IsFavoriteAsync(location.Id))
                await RemoveFavoriteAsync(location.Id);
            else
                await AddFavoriteAsync(location);
        }
    }
}
