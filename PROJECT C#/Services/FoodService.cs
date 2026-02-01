using PROJECT_C_.DTOs;
using PROJECT_C_.Services.Interfaces;
using PROJECT_C_.Data;
using Microsoft.EntityFrameworkCore;

using PROJECT_C_.Models;

namespace PROJECT_C_.Services
{
    public class FoodService : IFoodService
    {
        private readonly AppDbContext _context;
        private readonly IDistanceCalculator _distanceCalculator;

        public FoodService(
            AppDbContext context,
            IDistanceCalculator distanceCalculator)
        {
            _context = context;
            _distanceCalculator = distanceCalculator;
        }

        public PagedResult<FoodDto> GetNearestFoods(
            double lat,
            double lng,
            int page,
            int pageSize,
            string languageCode = "vi-VN")
        {
            // Normalize language code (e.g., "en-US,en;q=0.9" -> "en-US")
            var lang = languageCode.Split(',')[0].Trim();

            var foods = _context.Foods
                .AsNoTracking()
                .Include(f => f.Translations)
                .Include(f => f.AudioFiles)
                .Select(f => new
                {
                    f.Id,
                    f.Price,
                    f.Latitude,
                    f.Longitude,
                    f.ImageUrl,
                    f.MapLink,
                    f.Radius,
                    f.Priority,
                    f.TtsScript,
                    Audio = f.AudioFiles.FirstOrDefault(),
                    Transl = f.Translations.FirstOrDefault(t => t.LanguageCode == lang) 
                             ?? f.Translations.FirstOrDefault(t => t.LanguageCode == "vi-VN"),
                    DefaultName = f.Name,
                    DefaultDesc = f.Description
                })
                .ToList();

            var total = foods.Count;

            var items = foods
                .Select(f => new
                {
                    Food = f,
                    Km = _distanceCalculator.Calculate(
                        lat, lng, f.Latitude, f.Longitude)
                })
                .OrderBy(x => x.Km)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new FoodDto
                {
                    Id = x.Food.Id,
                    Name = x.Food.Transl?.Name ?? x.Food.DefaultName,
                    Description = x.Food.Transl?.Description ?? x.Food.DefaultDesc,
                    Price = x.Food.Price,
                    Latitude = x.Food.Latitude,
                    Longitude = x.Food.Longitude,
                    ImageUrl = x.Food.ImageUrl,
                    MapLink = x.Food.MapLink,
                    Radius = x.Food.Radius,
                    Priority = x.Food.Priority,
                    TtsScript = x.Food.TtsScript,
                    HasAudio = x.Food.Audio != null,
                    AudioUrl = x.Food.Audio != null ? $"/api/audio/{x.Food.Audio.Id}" : null,
                    Distance = x.Km < 1
                        ? Math.Round(x.Km * 1000.0, 0)
                        : Math.Round(x.Km, 2),
                    Unit = x.Km < 1 ? "m" : "km"
                })
                .ToList();

            return new PagedResult<FoodDto>(
                items,
                page,
                total,
                pageSize);
        }
        public async Task<Food> CreateFoodAsync(Food food)
        {
            _context.Foods.Add(food);
            await _context.SaveChangesAsync();
            return food;
        }

        public async Task<Food?> UpdateFoodAsync(int id, Food food)
        {
            var existing = await _context.Foods.FindAsync(id);
            if (existing == null) return null;

            existing.Name = food.Name;
            existing.Description = food.Description;
            existing.Price = food.Price;
            existing.Latitude = food.Latitude;
            existing.Longitude = food.Longitude;
            existing.ImageUrl = food.ImageUrl;
            existing.MapLink = food.MapLink;
            existing.Radius = food.Radius;
            existing.Priority = food.Priority;
            existing.TtsScript = food.TtsScript;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteFoodAsync(int id)
        {
            var food = await _context.Foods.FindAsync(id);
            if (food == null) return false;

            _context.Foods.Remove(food);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Food?> GetFoodByIdAsync(int id)
        {
            return await _context.Foods
                .Include(f => f.Translations)
                .Include(f => f.AudioFiles)
                .FirstOrDefaultAsync(f => f.Id == id);
        }
    }

}
