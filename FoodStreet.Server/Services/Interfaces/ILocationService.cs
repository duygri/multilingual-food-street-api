using PROJECT_C_.DTOs;
using PROJECT_C_.Models;

namespace PROJECT_C_.Services.Interfaces
{
    public interface ILocationService
    {
        // === GPS / Public ===
        PagedResult<LocationDto> GetNearestLocations(double lat, double lng, int page, int pageSize, string languageCode = "vi-VN");

        // === CRUD ===
        Task<Location> CreateLocationAsync(Location location);
        Task<Location?> UpdateLocationAsync(int id, Location location);
        Task<bool> DeleteLocationAsync(int id);
        Task<Location?> GetLocationByIdAsync(int id);

        // === Seller-specific ===
        Task<IEnumerable<Location>> GetLocationsByOwnerAsync(string ownerId);

        // === Admin-specific ===
        Task<IEnumerable<Location>> GetAllLocationsAsync();
        Task<IEnumerable<Location>> GetPendingLocationsAsync();
    }
}
