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

        public FoodService(AppDbContext context)
        {
            _context = context;
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
            existing.ImageUrl = food.ImageUrl;
            existing.LocationId = food.LocationId;
            existing.CategoryId = food.CategoryId;

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
                .Include(f => f.Location)
                .Include(f => f.Category)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        /// <summary>
        /// Lấy danh sách món ăn theo LocationId
        /// </summary>
        public async Task<IEnumerable<FoodDto>> GetFoodsByLocationAsync(int locationId)
        {
            return await _context.Foods
                .AsNoTracking()
                .Where(f => f.LocationId == locationId)
                .Include(f => f.Category)
                .Select(f => new FoodDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Description = f.Description,
                    Price = f.Price,
                    ImageUrl = f.ImageUrl,
                    LocationId = f.LocationId,
                    CategoryId = f.CategoryId,
                    CategoryName = f.Category != null ? f.Category.Name : null
                })
                .ToListAsync();
        }
    }
}
