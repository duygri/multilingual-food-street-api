using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PROJECT_C_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetFood()
        {
            var foods = new[]
            {
        new { Id = 1, Name = "Bánh mì", Description = "Vietnamese sandwich"},
        new { Id = 2, Name = "Phở", Description = "Pho"},
    };
            return Ok(foods);
        }
    }
}

