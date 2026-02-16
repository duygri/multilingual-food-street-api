using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using PROJECT_C_.DTOs;
using PROJECT_C_.Models;
using PROJECT_C_.Services.Interfaces;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;
        private readonly UserManager<IdentityUser> _userManager;

        public LocationController(
            ILocationService locationService,
            UserManager<IdentityUser> userManager)
        {
            _locationService = locationService;
            _userManager = userManager;
        }

        // ========================================
        // PUBLIC - GPS tìm địa điểm gần nhất
        // ========================================

        /// <summary>
        /// Tìm địa điểm gần nhất (public, chỉ trả về đã duyệt)
        /// </summary>
        [HttpGet("near")]
        public IActionResult GetNearestLocations(
            double lat,
            double lng,
            int page = 1,
            int pageSize = 10)
        {
            string languageCode = Request.Headers["Accept-Language"].ToString();
            if (string.IsNullOrEmpty(languageCode)) languageCode = "vi-VN";

            var result = _locationService.GetNearestLocations(
                lat, lng, page, pageSize, languageCode);

            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết 1 địa điểm (public)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LocationDto>> GetLocation(int id)
        {
            var location = await _locationService.GetLocationByIdAsync(id);
            if (location == null) return NotFound();

            return new LocationDto
            {
                Id = location.Id,
                Name = location.Name,
                Description = location.Description,
                Address = location.Address,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Radius = location.Radius,
                Priority = location.Priority,
                ImageUrl = location.ImageUrl,
                MapLink = location.MapLink,
                TtsScript = location.TtsScript,
                CategoryId = location.CategoryId,
                CategoryName = location.Category?.Name,
                IsApproved = location.IsApproved,
                FoodCount = location.Foods.Count
            };
        }

        // ========================================
        // SELLER - quản lý địa điểm của mình
        // ========================================

        /// <summary>
        /// Seller: Xem danh sách địa điểm của mình
        /// </summary>
        [HttpGet("my")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetMyLocations()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var locations = await _locationService.GetLocationsByOwnerAsync(userId);
            return Ok(locations.Select(MapToDto));
        }

        /// <summary>
        /// Seller: Tạo địa điểm mới (cần Admin duyệt)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<LocationDto>> CreateLocation([FromBody] LocationDto dto)
        {
            var userId = _userManager.GetUserId(User);

            var location = new Location
            {
                Name = dto.Name,
                Description = dto.Description,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Radius = dto.Radius,
                Priority = dto.Priority,
                ImageUrl = dto.ImageUrl,
                MapLink = dto.MapLink,
                TtsScript = dto.TtsScript,
                CategoryId = dto.CategoryId,
                OwnerId = userId,
                IsApproved = false // Seller tạo → chờ duyệt
            };

            var created = await _locationService.CreateLocationAsync(location);

            var resultDto = new LocationDto
            {
                Id = created.Id,
                Name = created.Name,
                Description = created.Description,
                Address = created.Address,
                Latitude = created.Latitude,
                Longitude = created.Longitude,
                IsApproved = created.IsApproved
            };

            return CreatedAtAction(nameof(GetLocation), new { id = resultDto.Id }, resultDto);
        }

        /// <summary>
        /// Seller: Cập nhật địa điểm của mình
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] LocationDto dto)
        {
            var existing = await _locationService.GetLocationByIdAsync(id);
            if (existing == null) return NotFound();

            // Seller chỉ được sửa địa điểm của mình
            if (User.IsInRole("Seller"))
            {
                var userId = _userManager.GetUserId(User);
                if (existing.OwnerId != userId)
                    return Forbid();
            }

            var location = new Location
            {
                Id = id,
                Name = dto.Name,
                Description = dto.Description,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Radius = dto.Radius,
                Priority = dto.Priority,
                ImageUrl = dto.ImageUrl,
                MapLink = dto.MapLink,
                TtsScript = dto.TtsScript,
                CategoryId = dto.CategoryId
            };

            var updated = await _locationService.UpdateLocationAsync(id, location);
            if (updated == null) return NotFound();

            return NoContent();
        }

        /// <summary>
        /// Seller/Admin: Xóa địa điểm
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            var existing = await _locationService.GetLocationByIdAsync(id);
            if (existing == null) return NotFound();

            // Seller chỉ được xóa địa điểm của mình
            if (User.IsInRole("Seller"))
            {
                var userId = _userManager.GetUserId(User);
                if (existing.OwnerId != userId)
                    return Forbid();
            }

            var success = await _locationService.DeleteLocationAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        // ========================================
        // ADMIN - quản lý tất cả địa điểm
        // ========================================

        /// <summary>
        /// Admin: Xem tất cả địa điểm
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetAllLocations()
        {
            var locations = await _locationService.GetAllLocationsAsync();
            return Ok(locations.Select(MapToDto));
        }

        /// <summary>
        /// Admin: Xem địa điểm chờ duyệt
        /// </summary>
        [HttpGet("admin/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetPendingLocations()
        {
            var locations = await _locationService.GetPendingLocationsAsync();
            return Ok(locations.Select(MapToDto));
        }

        /// <summary>
        /// Admin: Duyệt địa điểm
        /// </summary>
        [HttpPost("admin/{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveLocation(int id)
        {
            var location = await _locationService.GetLocationByIdAsync(id);
            if (location == null) return NotFound();

            location.IsApproved = true;
            location.ApprovedAt = DateTime.UtcNow;
            await _locationService.UpdateLocationAsync(id, location);

            return Ok(new { message = "Địa điểm đã được duyệt", locationId = id });
        }

        /// <summary>
        /// Admin: Từ chối địa điểm (xóa)
        /// </summary>
        [HttpPost("admin/{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectLocation(int id)
        {
            var location = await _locationService.GetLocationByIdAsync(id);
            if (location == null) return NotFound();

            await _locationService.DeleteLocationAsync(id);

            return Ok(new { message = "Địa điểm đã bị từ chối và xóa", locationId = id });
        }

        // ========================================
        // Helper: Map Location entity to DTO
        // ========================================
        private static LocationDto MapToDto(Location l) => new()
        {
            Id = l.Id,
            Name = l.Name,
            Description = l.Description,
            Address = l.Address,
            Latitude = l.Latitude,
            Longitude = l.Longitude,
            Radius = l.Radius,
            Priority = l.Priority,
            ImageUrl = l.ImageUrl,
            MapLink = l.MapLink,
            TtsScript = l.TtsScript,
            CategoryId = l.CategoryId,
            CategoryName = l.Category?.Name,
            IsApproved = l.IsApproved,
            FoodCount = l.Foods?.Count ?? 0
        };
    }
}
