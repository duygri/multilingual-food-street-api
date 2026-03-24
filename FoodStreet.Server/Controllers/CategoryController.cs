using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.Models;
using FoodStreet.Server.Extensions;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/category
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Icon = c.Icon,
                    Description = c.Description,
                    LocationCount = c.Locations.Count
                })
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/category/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Where(c => c.Id == id)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Icon = c.Icon,
                    Description = c.Description,
                    LocationCount = c.Locations.Count
                })
                .FirstOrDefaultAsync();

            if (category == null)
                return NotFound();

            return Ok(category);
        }

        // POST: api/category (Admin only)
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Category>> CreateCategory(CreateCategoryDto dto)
        {
            if (!User.IsAdminRole()) return Forbid();
            var category = new Category
            {
                Name = dto.Name,
                Icon = dto.Icon,
                Description = dto.Description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        // PUT: api/category/5 (Admin only)
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCategory(int id, CreateCategoryDto dto)
        {
            if (!User.IsAdminRole()) return Forbid();
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            category.Name = dto.Name;
            category.Icon = dto.Icon;
            category.Description = dto.Description;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/category/5 (Admin only)
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (!User.IsAdminRole()) return Forbid();
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Icon { get; set; }
        public string? Description { get; set; }
        public int LocationCount { get; set; }
    }

    public class CreateCategoryDto
    {
        public string Name { get; set; } = "";
        public string? Icon { get; set; }
        public string? Description { get; set; }
    }
}
