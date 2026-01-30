
﻿using Microsoft.AspNetCore.Mvc;
using PROJECT_C_.Services.Interfaces;

﻿using Microsoft.AspNetCore.Http;
using PROJECT_C_.DTOs;
using PROJECT_C_.Models;


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

        [HttpGet]
        public IActionResult GetFoods()
        {
             // Simplified generic getAll for admin testing, real app uses paging
             return Ok(_foodService.GetNearestFoods(0, 0, 1, 100)); 
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FoodDto>> GetFood(int id)
        {
            var food = await _foodService.GetFoodByIdAsync(id);
            if (food == null) return NotFound();
            
            return new FoodDto
            {
                Id = food.Id,
                Name = food.Name,
                Description = food.Description,
                Price = food.Price,
                Latitude = food.Latitude,
                Longitude = food.Longitude
            };
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult<FoodDto>> CreateFood([FromBody] FoodDto foodDto)
        {
            var food = new Food
            {
                Name = foodDto.Name,
                Description = foodDto.Description,
                Price = foodDto.Price,
                Latitude = foodDto.Latitude,
                Longitude = foodDto.Longitude
            };
            
            var created = await _foodService.CreateFoodAsync(food);
            
            var resultDto = new FoodDto
            {
                Id = created.Id,
                Name = created.Name,
                Description = created.Description,
                Price = created.Price,
                Latitude = created.Latitude,
                Longitude = created.Longitude
            };

            return CreatedAtAction(nameof(GetFood), new { id = resultDto.Id }, resultDto);
        }

        [HttpPut("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> UpdateFood(int id, [FromBody] FoodDto foodDto)
        {
            if (id != foodDto.Id && foodDto.Id != 0) return BadRequest();

            var food = new Food
            {
                Id = id,
                Name = foodDto.Name,
                Description = foodDto.Description,
                Price = foodDto.Price,
                Latitude = foodDto.Latitude,
                Longitude = foodDto.Longitude
            };

            var updated = await _foodService.UpdateFoodAsync(id, food);
            if (updated == null) return NotFound();
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> DeleteFood(int id)
        {
            var success = await _foodService.DeleteFoodAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
        // API GPS
        [HttpGet("near")]
        public IActionResult GetNearestFoods(
            double lat,
            double lng,
            int page = 1,
            int pageSize = 10)
        {
            string languageCode = Request.Headers["Accept-Language"].ToString();
            if (string.IsNullOrEmpty(languageCode)) languageCode = "vi-VN";

            var result = _foodService.GetNearestFoods(
                lat, lng, page, pageSize, languageCode);

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
