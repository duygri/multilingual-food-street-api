using PROJECT_C_.DTOs;
using PROJECT_C_.Services.Interfaces;
using PROJECT_C_.Data;
using Microsoft.EntityFrameworkCore;

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
            int pageSize)
        {
            var foods = _context.Foods
                .AsNoTracking()
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Description,
                    f.Price,
                    f.Latitude,
                    f.Longitude
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
                    Name = x.Food.Name,
                    Description = x.Food.Description ?? "",
                    Price = x.Food.Price,
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
    }
}
