using PROJECT_C_.DTOs;
using PROJECT_C_.Models;


namespace PROJECT_C_.Services.Interfaces
{
    public interface IFoodService
    {
        PagedResult<FoodDto> GetNearestFoods(double lat, double lng, int page, int pageSize, string languageCode = "vi-VN");
        Task<Food> CreateFoodAsync(Food food);
        Task<Food?> UpdateFoodAsync(int id, Food food);
        Task<bool> DeleteFoodAsync(int id);
        Task<Food?> GetFoodByIdAsync(int id);
    }
}
