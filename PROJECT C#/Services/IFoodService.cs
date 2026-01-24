using PROJECT_C_.DTOs;

public interface IFoodService
{
    List<FoodDto> GetNeareastFoods (double lat, double lng, int top );
}
