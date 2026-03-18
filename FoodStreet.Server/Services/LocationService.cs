using PROJECT_C_.DTOs;
using PROJECT_C_.Services.Interfaces;
using PROJECT_C_.Data;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Models;

namespace PROJECT_C_.Services
{
    public class LocationService : ILocationService
    {
        private readonly AppDbContext _context;
        private readonly IDistanceCalculator _distanceCalculator;

        public LocationService(
            AppDbContext context,
            IDistanceCalculator distanceCalculator)
        {
            _context = context;
            _distanceCalculator = distanceCalculator;
        }

        // ========================================
        // GPS / PUBLIC
        // ========================================
        public PagedResult<LocationDto> GetNearestLocations(
            double lat,
            double lng,
            int page,
            int pageSize,
            string languageCode = "vi-VN")
        {
            var lang = languageCode.Split(',')[0].Trim();

            var locations = _context.Locations
                .AsNoTracking()
                .Where(l => l.IsApproved) // Chỉ hiển thị địa điểm đã duyệt
                .Include(l => l.Translations)
                .Include(l => l.AudioFiles)
                .Include(l => l.Foods)
                .Include(l => l.Category)
                .Select(l => new
                {
                    l.Id,
                    l.Latitude,
                    l.Longitude,
                    l.ImageUrl,
                    l.MapLink,
                    l.Radius,
                    l.Priority,
                    l.TtsScript,
                    l.Address,
                    l.CategoryId,
                    CategoryName = l.Category != null ? l.Category.Name : null,
                    Audio = l.AudioFiles.FirstOrDefault(),
                    Transl = l.Translations.FirstOrDefault(t => t.LanguageCode == lang)
                             ?? l.Translations.FirstOrDefault(t => t.LanguageCode == "vi-VN"),
                    DefaultName = l.Name,
                    DefaultDesc = l.Description,
                    FoodCount = l.Foods.Count
                })
                .ToList();

            var total = locations.Count;

            var items = locations
                .Select(l => new
                {
                    Location = l,
                    Km = _distanceCalculator.Calculate(
                        lat, lng, l.Latitude, l.Longitude)
                })
                .OrderBy(x => x.Km)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new LocationDto
                {
                    Id = x.Location.Id,
                    Name = lang == "vi-VN"
                        ? x.Location.DefaultName
                        : (x.Location.Transl?.Name ?? x.Location.DefaultName),
                    Description = lang == "vi-VN"
                        ? x.Location.DefaultDesc
                        : (x.Location.Transl?.Description ?? x.Location.DefaultDesc),
                    Address = x.Location.Address,
                    Latitude = x.Location.Latitude,
                    Longitude = x.Location.Longitude,
                    ImageUrl = x.Location.ImageUrl,
                    MapLink = x.Location.MapLink,
                    Radius = x.Location.Radius,
                    Priority = x.Location.Priority,
                    TtsScript = x.Location.TtsScript,
                    CategoryId = x.Location.CategoryId,
                    CategoryName = x.Location.CategoryName,
                    HasAudio = x.Location.Audio != null,
                    AudioUrl = x.Location.Audio != null ? $"/api/audio/{x.Location.Audio.Id}" : null,
                    IsApproved = true,
                    FoodCount = x.Location.FoodCount,
                    Distance = x.Km < 1
                        ? Math.Round(x.Km * 1000.0, 0)
                        : Math.Round(x.Km, 2),
                    Unit = x.Km < 1 ? "m" : "km"
                })
                .ToList();

            return new PagedResult<LocationDto>(
                items,
                page,
                total,
                pageSize);
        }

        // ========================================
        // CRUD
        // ========================================
        public async Task<Location> CreateLocationAsync(Location location)
        {
            _context.Locations.Add(location);
            await _context.SaveChangesAsync();
            return location;
        }

        public async Task<Location?> UpdateLocationAsync(int id, Location location)
        {
            var existing = await _context.Locations.FindAsync(id);
            if (existing == null) return null;

            existing.Name = location.Name;
            existing.Description = location.Description;
            existing.Address = location.Address;
            existing.Latitude = location.Latitude;
            existing.Longitude = location.Longitude;
            existing.ImageUrl = location.ImageUrl;
            existing.MapLink = location.MapLink;
            existing.Radius = location.Radius;
            existing.Priority = location.Priority;
            existing.TtsScript = location.TtsScript;
            existing.CategoryId = location.CategoryId;

            await _context.SaveChangesAsync();
            return existing;
        }

        /// <summary>
        /// Chỉ dùng để Admin approve — không dùng UpdateLocationAsync vì method đó không persist IsApproved.
        /// </summary>
        public async Task<bool> ApproveLocationAsync(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return false;

            location.IsApproved = true;
            location.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteLocationAsync(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return false;

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Location?> GetLocationByIdAsync(int id)
        {
            return await _context.Locations
                .Include(l => l.Translations)
                .Include(l => l.AudioFiles)
                .Include(l => l.Foods)
                .Include(l => l.Category)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        // ========================================
        // SELLER - chỉ xem địa điểm của mình
        // ========================================
        public async Task<IEnumerable<Location>> GetLocationsByOwnerAsync(string ownerId)
        {
            return await _context.Locations
                .Include(l => l.Category)
                .Include(l => l.Foods)
                .Where(l => l.OwnerId == ownerId)
                .OrderByDescending(l => l.Id)
                .ToListAsync();
        }

        // ========================================
        // ADMIN - xem tất cả
        // ========================================
        public async Task<IEnumerable<Location>> GetAllLocationsAsync()
        {
            return await _context.Locations
                .Include(l => l.Category)
                .Include(l => l.Foods)
                .OrderByDescending(l => l.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetPendingLocationsAsync()
        {
            return await _context.Locations
                .Include(l => l.Category)
                .Where(l => !l.IsApproved)
                .OrderByDescending(l => l.Id)
                .ToListAsync();
        }
    }
}
