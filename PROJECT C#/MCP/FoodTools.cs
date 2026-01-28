using ModelContextProtocol.Server;
using PROJECT_C_.Services.Interfaces;
using System.ComponentModel;

namespace PROJECT_C_.MCP;

[McpServerToolType]
public class FoodTools
{
    private readonly IFoodService _foodService;

    public FoodTools(IFoodService foodService)
    {
        _foodService = foodService;
    }

    [McpServerTool, Description("Get nearest foods based on GPS location")]
    public object GetNearestFoods(
        double lat,
        double lng,
        int page = 1,
        int pageSize = 10)
    {
        return _foodService.GetNearestFoods(lat, lng, page, pageSize);
    }
}
