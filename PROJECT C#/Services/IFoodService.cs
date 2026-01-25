using PROJECT_C_.DTOs;

public interface IFoodService
{
    List<FoodDto> GetNearestFoods (double lat, double lng, int top );
}
