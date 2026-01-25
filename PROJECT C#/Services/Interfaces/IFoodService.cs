using PROJECT_C_.DTOs;

namespace PROJECT_C_.Services.Interfaces
{
    public interface IFoodService
    {
        PagedResult<FoodDto> GetNearestFoods(double lat, double lng, int page, int pageSize);
    }
}
