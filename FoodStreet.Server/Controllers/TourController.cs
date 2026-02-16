using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.Models;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TourController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TourController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách tất cả Tours
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllTours([FromQuery] bool? activeOnly = null)
        {
            var query = _context.Tours.AsQueryable();

            if (activeOnly == true)
            {
                query = query.Where(t => t.IsActive);
            }

            var tours = await query
                .Include(t => t.Items)
                    .ThenInclude(i => i.Food)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Description,
                    t.EstimatedDurationMinutes,
                    t.EstimatedDistanceKm,
                    t.IsActive,
                    t.CreatedAt,
                    ItemCount = t.Items.Count,
                    Items = t.Items.OrderBy(i => i.Order).Select(i => new
                    {
                        i.Id,
                        i.FoodId,
                        FoodName = i.Food != null ? i.Food.Name : "Unknown",
                        i.Order,
                        i.Note,
                        i.EstimatedStopMinutes
                    }).ToList()
                })
                .ToListAsync();

            return Ok(tours);
        }

        /// <summary>
        /// Lấy chi tiết một Tour
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTour(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.Items)
                    .ThenInclude(i => i.Food)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
                return NotFound(new { message = "Tour không tồn tại" });

            var result = new
            {
                tour.Id,
                tour.Name,
                tour.Description,
                tour.EstimatedDurationMinutes,
                tour.EstimatedDistanceKm,
                tour.IsActive,
                tour.CreatedAt,
                tour.UpdatedAt,
                Items = tour.Items.OrderBy(i => i.Order).Select(i => new
                {
                    i.Id,
                    i.FoodId,
                    FoodName = i.Food?.Name,
                    FoodImageUrl = i.Food?.ImageUrl,
                    FoodLatitude = i.Food != null && i.Food.Location != null ? i.Food.Location.Latitude : (double?)null,
                    FoodLongitude = i.Food != null && i.Food.Location != null ? i.Food.Location.Longitude : (double?)null,
                    i.Order,
                    i.Note,
                    i.EstimatedStopMinutes
                }).ToList()
            };

            return Ok(result);
        }

        /// <summary>
        /// Tạo Tour mới
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateTour([FromBody] CreateTourRequest request)
        {
            var tour = new Tour
            {
                Name = request.Name,
                Description = request.Description,
                EstimatedDurationMinutes = request.EstimatedDurationMinutes,
                EstimatedDistanceKm = request.EstimatedDistanceKm,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            // Thêm các items nếu có
            if (request.FoodIds != null && request.FoodIds.Any())
            {
                int order = 1;
                foreach (var foodId in request.FoodIds)
                {
                    var item = new TourItem
                    {
                        TourId = tour.Id,
                        FoodId = foodId,
                        Order = order++,
                        EstimatedStopMinutes = 15
                    };
                    _context.TourItems.Add(item);
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { id = tour.Id, message = "Tạo Tour thành công" });
        }

        /// <summary>
        /// Cập nhật Tour
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateTour(int id, [FromBody] UpdateTourRequest request)
        {
            var tour = await _context.Tours
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
                return NotFound(new { message = "Tour không tồn tại" });

            tour.Name = request.Name;
            tour.Description = request.Description;
            tour.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
            tour.EstimatedDistanceKm = request.EstimatedDistanceKm;
            tour.IsActive = request.IsActive;
            tour.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật Tour thành công" });
        }

        /// <summary>
        /// Xóa Tour
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
                return NotFound(new { message = "Tour không tồn tại" });

            // Xóa các items trước
            _context.TourItems.RemoveRange(tour.Items);
            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa Tour thành công" });
        }

        /// <summary>
        /// Thêm điểm dừng vào Tour
        /// </summary>
        [HttpPost("{id}/items")]
        [Authorize]
        public async Task<IActionResult> AddTourItem(int id, [FromBody] AddTourItemRequest request)
        {
            var tour = await _context.Tours
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
                return NotFound(new { message = "Tour không tồn tại" });

            var food = await _context.Foods.FindAsync(request.FoodId);
            if (food == null)
                return NotFound(new { message = "Món ăn không tồn tại" });

            var maxOrder = tour.Items.Any() ? tour.Items.Max(i => i.Order) : 0;

            var item = new TourItem
            {
                TourId = id,
                FoodId = request.FoodId,
                Order = maxOrder + 1,
                Note = request.Note,
                EstimatedStopMinutes = request.EstimatedStopMinutes
            };

            _context.TourItems.Add(item);
            await _context.SaveChangesAsync();

            return Ok(new { id = item.Id, message = "Thêm điểm dừng thành công" });
        }

        /// <summary>
        /// Xóa điểm dừng khỏi Tour
        /// </summary>
        [HttpDelete("{tourId}/items/{itemId}")]
        [Authorize]
        public async Task<IActionResult> RemoveTourItem(int tourId, int itemId)
        {
            var item = await _context.TourItems
                .FirstOrDefaultAsync(i => i.Id == itemId && i.TourId == tourId);

            if (item == null)
                return NotFound(new { message = "Điểm dừng không tồn tại" });

            _context.TourItems.Remove(item);
            await _context.SaveChangesAsync();

            // Cập nhật lại thứ tự
            var remainingItems = await _context.TourItems
                .Where(i => i.TourId == tourId)
                .OrderBy(i => i.Order)
                .ToListAsync();

            int newOrder = 1;
            foreach (var ri in remainingItems)
            {
                ri.Order = newOrder++;
            }
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa điểm dừng thành công" });
        }

        /// <summary>
        /// Cập nhật thứ tự các điểm dừng trong Tour
        /// </summary>
        [HttpPut("{id}/items/reorder")]
        [Authorize]
        public async Task<IActionResult> ReorderTourItems(int id, [FromBody] ReorderItemsRequest request)
        {
            var tour = await _context.Tours
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
                return NotFound(new { message = "Tour không tồn tại" });

            foreach (var itemOrder in request.ItemOrders)
            {
                var item = tour.Items.FirstOrDefault(i => i.Id == itemOrder.ItemId);
                if (item != null)
                {
                    item.Order = itemOrder.Order;
                }
            }

            tour.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thứ tự thành công" });
        }

        /// <summary>
        /// Toggle trạng thái Active của Tour
        /// </summary>
        [HttpPatch("{id}/toggle")]
        [Authorize]
        public async Task<IActionResult> ToggleTourActive(int id)
        {
            var tour = await _context.Tours.FindAsync(id);

            if (tour == null)
                return NotFound(new { message = "Tour không tồn tại" });

            tour.IsActive = !tour.IsActive;
            tour.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { isActive = tour.IsActive, message = tour.IsActive ? "Đã kích hoạt Tour" : "Đã tạm dừng Tour" });
        }
    }

    // Request DTOs
    public class CreateTourRequest
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public int EstimatedDurationMinutes { get; set; } = 60;
        public double EstimatedDistanceKm { get; set; } = 1.0;
        public bool IsActive { get; set; } = true;
        public List<int>? FoodIds { get; set; }
    }

    public class UpdateTourRequest
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public int EstimatedDurationMinutes { get; set; }
        public double EstimatedDistanceKm { get; set; }
        public bool IsActive { get; set; }
    }

    public class AddTourItemRequest
    {
        public int FoodId { get; set; }
        public string? Note { get; set; }
        public int EstimatedStopMinutes { get; set; } = 15;
    }

    public class ReorderItemsRequest
    {
        public List<ItemOrderDto> ItemOrders { get; set; } = new();
    }

    public class ItemOrderDto
    {
        public int ItemId { get; set; }
        public int Order { get; set; }
    }
}
