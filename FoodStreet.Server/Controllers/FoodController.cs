
﻿using Microsoft.AspNetCore.Mvc;
using PROJECT_C_.Services.Interfaces;

﻿using Microsoft.AspNetCore.Http;
using PROJECT_C_.DTOs;
using PROJECT_C_.Models;


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoodController : ControllerBase
    {
        private readonly IFoodService _foodService;
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public FoodController(AppDbContext context, UserManager<IdentityUser> userManager, IFoodService foodService)

        {
            _context = context;
            _userManager = userManager;
            _foodService = foodService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Food>>> GetFoods()
        {
            var query = _context.Foods.Include(f => f.Translations).Include(f => f.Category).AsQueryable();

            // Socratic Logic: Seller Isolation
            // Only apply filter if User IS authenticated and IS a Seller
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Seller"))
            {
                var userId = _userManager.GetUserId(User);
                query = query.Where(f => f.OwnerId == userId);
            }

            return await query.ToListAsync();
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
        [Authorize(Roles = "Admin,Seller")]
        public async Task<ActionResult<FoodDto>> CreateFood([FromBody] FoodDto foodDto)
        {
            var userId = _userManager.GetUserId(User);

            var food = new Food
            {
                Name = foodDto.Name,
                Description = foodDto.Description,
                Price = foodDto.Price,
                Latitude = foodDto.Latitude,
                Longitude = foodDto.Longitude,
                OwnerId = userId,
                CategoryId = foodDto.CategoryId
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
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> UpdateFood(int id, [FromBody] FoodDto foodDto)
        {
            if (id != foodDto.Id && foodDto.Id != 0) return BadRequest();

            var existingFood = await _context.Foods.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (existingFood == null) return NotFound();

            // Ownership Check
            if (User.IsInRole("Seller"))
            {
                var userId = _userManager.GetUserId(User);
                if (existingFood.OwnerId != userId)
                {
                    return Forbid(); 
                }
            }

            var food = new Food
            {
                Id = id,
                Name = foodDto.Name,
                Description = foodDto.Description,
                Price = foodDto.Price,
                Latitude = foodDto.Latitude,
                Longitude = foodDto.Longitude,
                CategoryId = foodDto.CategoryId
            };

            var updated = await _foodService.UpdateFoodAsync(id, food);
            if (updated == null) return NotFound();
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> DeleteFood(int id)
        {
            var food = await _context.Foods.FindAsync(id);
            if (food == null) return NotFound();

            // Ownership Check
            if (User.IsInRole("Seller"))
            {
                var userId = _userManager.GetUserId(User);
                if (food.OwnerId != userId)
                {
                    return Forbid();
                }
            }

            // Use context to remove to ensure consistency with controller logic, 
            // OR use service if it has extra logic. Service usually has logic.
            // But we already fetched 'food' from context.
            // Let's use service to be safe about business logic, but we passed the auth check.
            
            var success = await _foodService.DeleteFoodAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        // GET: api/food/pending (Admin only - get foods awaiting approval)
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Food>>> GetPendingFoods()
        {
            var pendingFoods = await _context.Foods
                .Include(f => f.Category)
                .Where(f => !f.IsApproved)
                .OrderByDescending(f => f.Id)
                .ToListAsync();

            return Ok(pendingFoods);
        }

        // POST: api/food/{id}/approve (Admin only)
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveFood(int id)
        {
            var food = await _context.Foods.FindAsync(id);
            if (food == null) return NotFound();

            food.IsApproved = true;
            food.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Món ăn đã được duyệt", foodId = id });
        }

        // POST: api/food/{id}/reject (Admin only - delete the food)
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectFood(int id)
        {
            var food = await _context.Foods.FindAsync(id);
            if (food == null) return NotFound();

            _context.Foods.Remove(food);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Món ăn đã bị từ chối và xóa", foodId = id });
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
