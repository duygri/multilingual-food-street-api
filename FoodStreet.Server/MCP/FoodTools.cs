using ModelContextProtocol.Server;
using PROJECT_C_.Services.Interfaces;
using System.ComponentModel;

namespace PROJECT_C_.MCP;

[McpServerToolType]
public class FoodTools
{
    private readonly ILocationService _locationService;

    public FoodTools(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [McpServerTool, Description("Get nearest locations/POIs based on GPS coordinates")]
    public object GetNearestLocations(
        double lat,
        double lng,
        int page = 1,
        int pageSize = 10)
    {
        return _locationService.GetNearestLocations(lat, lng, page, pageSize);
    }
}
