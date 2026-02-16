
using Microsoft.AspNetCore.Mvc;
using PROJECT_C_.Services.Interfaces;
using Microsoft.AspNetCore.Http;
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

        /// <summary>
        /// Lấy tất cả món ăn (Admin xem tất cả, Seller xem theo Location của mình)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Food>>> GetFoods()
        {
            var query = _context.Foods
                .Include(f => f.Translations)
                .Include(f => f.Category)
                .Include(f => f.Location)
                .AsQueryable();

            // Seller chỉ xem món ăn thuộc Location của mình
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Seller"))
            {
                var userId = _userManager.GetUserId(User);
                query = query.Where(f => f.Location != null && f.Location.OwnerId == userId);
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách món ăn theo địa điểm
        /// </summary>
        [HttpGet("by-location/{locationId}")]
        public async Task<ActionResult<IEnumerable<FoodDto>>> GetFoodsByLocation(int locationId)
        {
            var foods = await _foodService.GetFoodsByLocationAsync(locationId);
            return Ok(foods);
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
                ImageUrl = food.ImageUrl,
                LocationId = food.LocationId,
                LocationName = food.Location?.Name,
                CategoryId = food.CategoryId,
                CategoryName = food.Category?.Name
            };
        }

        /// <summary>
        /// Seller: Tạo món ăn (phải thuộc về 1 Location của mình)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<ActionResult<FoodDto>> CreateFood([FromBody] FoodDto foodDto)
        {
            // Kiểm tra Location ownership nếu là Seller
            if (User.IsInRole("Seller") && foodDto.LocationId.HasValue)
            {
                var userId = _userManager.GetUserId(User);
                var location = await _context.Locations.FindAsync(foodDto.LocationId.Value);
                if (location == null || location.OwnerId != userId)
                    return Forbid();
            }

            var food = new Food
            {
                Name = foodDto.Name,
                Description = foodDto.Description,
                Price = foodDto.Price,
                ImageUrl = foodDto.ImageUrl,
                LocationId = foodDto.LocationId,
                CategoryId = foodDto.CategoryId
            };
            
            var created = await _foodService.CreateFoodAsync(food);
            
            var resultDto = new FoodDto
            {
                Id = created.Id,
                Name = created.Name,
                Description = created.Description,
                Price = created.Price,
                ImageUrl = created.ImageUrl,
                LocationId = created.LocationId
            };

            return CreatedAtAction(nameof(GetFood), new { id = resultDto.Id }, resultDto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> UpdateFood(int id, [FromBody] FoodDto foodDto)
        {
            if (id != foodDto.Id && foodDto.Id != 0) return BadRequest();

            var existingFood = await _context.Foods
                .Include(f => f.Location)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);
            if (existingFood == null) return NotFound();

            // Seller ownership check qua Location
            if (User.IsInRole("Seller"))
            {
                var userId = _userManager.GetUserId(User);
                if (existingFood.Location?.OwnerId != userId)
                    return Forbid(); 
            }

            var food = new Food
            {
                Id = id,
                Name = foodDto.Name,
                Description = foodDto.Description,
                Price = foodDto.Price,
                ImageUrl = foodDto.ImageUrl,
                LocationId = foodDto.LocationId,
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
            var food = await _context.Foods
                .Include(f => f.Location)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (food == null) return NotFound();

            // Seller ownership check qua Location
            if (User.IsInRole("Seller"))
            {
                var userId = _userManager.GetUserId(User);
                if (food.Location?.OwnerId != userId)
                    return Forbid();
            }

            var success = await _foodService.DeleteFoodAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
