using PROJECT_C_.DTOs;
using PROJECT_C_.Models;


namespace PROJECT_C_.Services.Interfaces
{
    public interface IFoodService
    {
        Task<Food> CreateFoodAsync(Food food);
        Task<Food?> UpdateFoodAsync(int id, Food food);
        Task<bool> DeleteFoodAsync(int id);
        Task<Food?> GetFoodByIdAsync(int id);
        Task<IEnumerable<FoodDto>> GetFoodsByLocationAsync(int locationId);
    }
}
