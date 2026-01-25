using Microsoft.AspNetCore.Mvc;
using PROJECT_C_.Services.Interfaces;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoodController : ControllerBase
    {
        private readonly IFoodService _foodService;

        public FoodController(IFoodService foodService)
        {
            _foodService = foodService;
        }

        [HttpGet("near")]
        public IActionResult GetNearest(
            double lat,
            double lng,
            int page = 1,
            int pageSize = 3)
        {
            var result = _foodService.GetNearestFoods(lat, lng, page, pageSize);
            return Ok(result);
        }
    }
}
