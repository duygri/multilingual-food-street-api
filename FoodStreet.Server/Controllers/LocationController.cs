using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using PROJECT_C_.DTOs;
using PROJECT_C_.Models;
using PROJECT_C_.Data;
using PROJECT_C_.Services.Interfaces;
using FoodStreet.Server.Extensions;
using FoodStreet.Server.Hubs;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly AppDbContext _dbContext;

        public LocationController(
            ILocationService locationService,
            UserManager<IdentityUser> userManager,
            IHubContext<NotificationHub> hubContext,
            AppDbContext dbContext)
        {
            _locationService = locationService;
            _userManager = userManager;
            _hubContext = hubContext;
            _dbContext = dbContext;
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
        [Authorize]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetMyLocations()
        {
            if (!User.IsSellerRole()) return Forbid();
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized();

            var locations = await _locationService.GetLocationsByOwnerAsync(userId);
            return Ok(locations.Select(MapToDto));
        }

        /// <summary>
        /// Seller: Tạo địa điểm mới (cần Admin duyệt)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<LocationDto>> CreateLocation([FromBody] LocationDto dto)
        {
            if (!User.IsSellerRole()) return Forbid();
            var userId = User.GetUserId();

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

            // Gửi thông báo cho Admin
            var senderName = User.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == System.Security.Claims.ClaimTypes.Name)?.Value ?? "Seller";
            var notification = new Notification
            {
                TargetRole = "Admin",
                Title = "POI mới cần duyệt",
                Message = $"{senderName} gửi yêu cầu tạo POI \"{created.Name}\"",
                Type = NotificationType.POI_Created,
                RelatedId = created.Id,
                SenderName = senderName
            };
            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();

            await _hubContext.Clients.Group("role_Admin").SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                Type = notification.Type.ToString(),
                notification.CreatedAt,
                notification.RelatedId,
                notification.SenderName
            });

            return CreatedAtAction(nameof(GetLocation), new { id = resultDto.Id }, resultDto);
        }

        /// <summary>
        /// Seller: Cập nhật địa điểm của mình
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] LocationDto dto)
        {
            if (!User.IsAdminRole() && !User.IsSellerRole()) return Forbid();
            var existing = await _locationService.GetLocationByIdAsync(id);
            if (existing == null) return NotFound();

            // Seller chỉ được sửa địa điểm của mình
            if (User.IsSellerRole())
            {
                var userId = User.GetUserId();
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
        [Authorize]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            if (!User.IsAdminRole() && !User.IsSellerRole()) return Forbid();
            var existing = await _locationService.GetLocationByIdAsync(id);
            if (existing == null) return NotFound();

            // Seller chỉ được xóa địa điểm của mình
            if (User.IsSellerRole())
            {
                var userId = User.GetUserId();
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
        [Authorize]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetAllLocations()
        {
            if (!User.IsAdminRole()) return Forbid();
            var locations = await _locationService.GetAllLocationsAsync();
            return Ok(locations.Select(MapToDto));
        }

        /// <summary>
        /// Admin: Xem địa điểm chờ duyệt
        /// </summary>
        [HttpGet("admin/pending")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetPendingLocations()
        {
            if (!User.IsAdminRole()) return Forbid();
            var locations = await _locationService.GetPendingLocationsAsync();
            return Ok(locations.Select(MapToDto));
        }

        /// <summary>
        /// Admin: Duyệt địa điểm
        /// </summary>
        [HttpPost("admin/{id}/approve")]
        [Authorize]
        public async Task<IActionResult> ApproveLocation(int id)
        {
            if (!User.IsAdminRole()) return Forbid();
            var location = await _locationService.GetLocationByIdAsync(id);
            if (location == null) return NotFound();

            location.IsApproved = true;
            location.ApprovedAt = DateTime.UtcNow;
            await _locationService.UpdateLocationAsync(id, location);

            // Gửi thông báo cho Seller (chủ POI)
            if (!string.IsNullOrEmpty(location.OwnerId))
            {
                var notification = new Notification
                {
                    UserId = location.OwnerId,
                    Title = "POI đã được duyệt ✅",
                    Message = $"POI \"{location.Name}\" đã được Admin duyệt và hiển thị công khai.",
                    Type = NotificationType.POI_Approved,
                    RelatedId = id,
                    SenderName = "Admin"
                };
                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync();

                await _hubContext.Clients.Group($"user_{location.OwnerId}").SendAsync("ReceiveNotification", new
                {
                    notification.Id,
                    notification.Title,
                    notification.Message,
                    Type = notification.Type.ToString(),
                    notification.CreatedAt,
                    notification.RelatedId,
                    notification.SenderName
                });
            }

            return Ok(new { message = "Địa điểm đã được duyệt", locationId = id });
        }

        /// <summary>
        /// Admin: Từ chối địa điểm (xóa)
        /// </summary>
        [HttpPost("admin/{id}/reject")]
        [Authorize]
        public async Task<IActionResult> RejectLocation(int id)
        {
            if (!User.IsAdminRole()) return Forbid();
            var location = await _locationService.GetLocationByIdAsync(id);
            if (location == null) return NotFound();

            // Gửi thông báo cho Seller trước khi xóa
            if (!string.IsNullOrEmpty(location.OwnerId))
            {
                var notification = new Notification
                {
                    UserId = location.OwnerId,
                    Title = "POI bị từ chối ❌",
                    Message = $"POI \"{location.Name}\" đã bị Admin từ chối.",
                    Type = NotificationType.POI_Rejected,
                    RelatedId = id,
                    SenderName = "Admin"
                };
                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync();

                await _hubContext.Clients.Group($"user_{location.OwnerId}").SendAsync("ReceiveNotification", new
                {
                    notification.Id,
                    notification.Title,
                    notification.Message,
                    Type = notification.Type.ToString(),
                    notification.CreatedAt,
                    notification.RelatedId,
                    notification.SenderName
                });
            }

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
