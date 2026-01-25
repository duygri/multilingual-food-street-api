using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PROJECT_C_.DTOs;
using PROJECT_C_.Models;

namespace PROJECT_C_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodController : ControllerBase
    {
        private readonly IFoodService _foodService;

        public FoodController(IFoodService foodService)
        {
            _foodService = foodService;
        }

        [HttpGet]
        public IActionResult GetFood()
        {

            var foods = new List<Food>
            {
                new Food
                {
                    Id = 1,
                    Name = "Banh mi",
                    Description = "Banh mi",
                    Latitude = 10.762622,
                    Longitude  = 106.660172
                },
                new Food
                {
                    Id = 2,
                    Name = "Pho",
                    Description = "Pho",
                    Latitude = 10.776889,
                    Longitude = 106.700806
                }
            };
            return Ok(foods);
        }
        // API GPS
        [HttpGet("near")]
        public IActionResult GetFoodNear(
            [FromQuery] double lat,
            [FromQuery] double lng,
            [FromQuery] int top = 3)
        {
            if(lat == 0 || lng == 0)
            {
                return BadRequest("Latitude and Longitude are required.");
            }

            var foods = _foodService.GetNearestFoods(lat, lng, top);

            var response = new ApiResponse<List<FoodDto>>
            {
                Total = foods.Count,
                Data = foods
            };

            return Ok(response);
        }
        
        // GPS CALCULATOR
        private double CalculateDistance(
            double lat1, double lng1,
            double lat2, double lng2)
        {
            double R = 6371; // Radius of the earth in km
            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);

            var a = Math.Sin(dLat/2) * Math.Sin(dLat/2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * 
                Math.Sin(dLng/2) * Math.Sin(dLng/2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
            return R * c; // Distance in km
        }
        private double ToRadians(double deg)
        {
            return deg * (Math.PI / 180);
        }
    }
}

