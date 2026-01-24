using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PROJECT_C_.Models;

namespace PROJECT_C_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodController : ControllerBase
    {
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
        public IActionResult GetFoodNear(double lat, double lng)
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
            var result = foods
                .Select(f =>
                {
                    var km = CalculateDistance(lat, lng, f.Latitude, f.Longitude);
                    return new
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Distance = km < 1 ? Math.Round(km * 1000, 0) : Math.Round(km, 2),
                        Unit = km < 1 ? "m" : "km"
                    };
                })
                .OrderBy(f => f.Distance)
                .Take(3)
                .ToList();
            return Ok(result);
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

