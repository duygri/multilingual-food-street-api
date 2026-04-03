using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using PROJECT_C_.DTOs;
using PROJECT_C_.Models;
using PROJECT_C_.Data;
using PROJECT_C_.Services.Interfaces;
using FoodStreet.Server.Constants;
using FoodStreet.Server.Extensions;
using FoodStreet.Server.Hubs;
using FoodStreet.Server.Mapping;
using FoodStreet.Server.Services.Audio;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/maps/locations")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly AppDbContext _dbContext;
        private readonly AudioTaskManager _audioTaskManager;

        public LocationController(
            ILocationService locationService,
            UserManager<IdentityUser> userManager,
            IHubContext<NotificationHub> hubContext,
            AppDbContext dbContext,
            AudioTaskManager audioTaskManager)
        {
            _locationService = locationService;
            _userManager = userManager;
            _hubContext = hubContext;
            _dbContext = dbContext;
            _audioTaskManager = audioTaskManager;
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
        /// Public: Lấy tất cả địa điểm đã duyệt (cho app mobile/user)
        /// </summary>
        [HttpGet("approved")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetApprovedLocations()
        {
            var locations = await _locationService.GetAllLocationsAsync();
            var approved = locations
                .Where(l => l.IsApproved)
                .Select(location => MapToDto(location, ResolveLanguage()));
            return Ok(approved);
        }

        /// <summary>
        /// Lấy thông tin chi tiết 1 địa điểm (public)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LocationDto>> GetLocation(int id)
        {
            var location = await _locationService.GetLocationByIdAsync(id);
            if (location == null) return NotFound();

            return MapToDto(location, ResolveLanguage());
        }

        // ========================================
        // POI OWNER - quản lý địa điểm của mình
        // ========================================

        /// <summary>
        /// POI Owner: Xem danh sách địa điểm của mình
        /// </summary>
        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetMyLocations()
        {
            if (!User.IsPoiOwnerRole()) return Forbid();
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized();

            var locations = await _locationService.GetLocationsByOwnerAsync(userId);
            return Ok(locations.Select(location => MapToDto(location, ResolveLanguage())));
        }

        /// <summary>
        /// POI Owner: Tạo địa điểm mới (cần Admin duyệt)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<LocationDto>> CreateLocation([FromBody] LocationDto dto)
        {
            if (!User.IsPoiOwnerRole()) return Forbid();
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

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
                IsApproved = false // POI Owner tạo → chờ duyệt
            };

            var created = await _locationService.CreateLocationAsync(location);
            var senderName = GetActorDisplayName("POI Owner");

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

            var notification = await CreateNotificationAsync(
                targetRole: AppRoles.Admin,
                title: "POI mới cần duyệt",
                message: $"{senderName} gửi yêu cầu tạo POI \"{created.Name}\"",
                type: NotificationType.POI_Created,
                relatedId: created.Id,
                senderName: senderName);

            await SendNotificationToRoleAsync(notification, AppRoles.Admin);
            await PublishPoiRealtimeToRoleAsync(
                AppRoles.Admin,
                NotificationHubEvents.PoiUpdated,
                created,
                "pending_review",
                "POI mới",
                $"POI \"{created.Name}\" vừa được gửi lên để kiểm duyệt.",
                senderName);
            await PublishPoiRealtimeToRoleAsync(
                AppRoles.Admin,
                NotificationHubEvents.ModerationChanged,
                created,
                "pending_review",
                "POI chờ duyệt",
                $"POI \"{created.Name}\" đang ở trạng thái chờ duyệt.",
                senderName);

            return CreatedAtAction(nameof(GetLocation), new { id = resultDto.Id }, resultDto);
        }

        /// <summary>
        /// POI Owner: Cập nhật địa điểm của mình
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] LocationDto dto)
        {
            if (!User.IsAdminRole() && !User.IsPoiOwnerRole()) return Forbid();
            var existing = await _locationService.GetLocationByIdAsync(id);
            if (existing == null) return NotFound();
            var isOwnerEdit = User.IsPoiOwnerRole();
            var actorName = GetActorDisplayName(isOwnerEdit ? "POI Owner" : "Admin");

            // POI Owner chỉ được sửa địa điểm của mình
            if (isOwnerEdit)
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

            var updated = await _locationService.UpdateLocationAsync(id, location, resetApproval: isOwnerEdit);
            if (updated == null) return NotFound();

            if (isOwnerEdit)
            {
                var notification = await CreateNotificationAsync(
                    targetRole: AppRoles.Admin,
                    title: "POI cần duyệt lại",
                    message: $"{actorName} đã cập nhật POI \"{updated.Name}\" và gửi kiểm duyệt lại.",
                    type: NotificationType.POI_Updated,
                    relatedId: updated.Id,
                    senderName: actorName);

                await SendNotificationToRoleAsync(notification, AppRoles.Admin);
                await PublishPoiRealtimeToRoleAsync(
                    AppRoles.Admin,
                    NotificationHubEvents.ModerationChanged,
                    updated,
                    "pending_review",
                    "POI cần duyệt lại",
                    $"POI \"{updated.Name}\" vừa được chỉnh sửa và đang chờ duyệt lại.",
                    actorName);
            }

            await PublishPoiRealtimeToPoiAsync(
                updated,
                NotificationHubEvents.PoiUpdated,
                updated.IsApproved ? "updated" : "pending_review",
                "POI đã cập nhật",
                $"POI \"{updated.Name}\" vừa được cập nhật.",
                actorName);

            if (!string.IsNullOrWhiteSpace(updated.OwnerId))
            {
                await PublishPoiRealtimeToUserAsync(
                    updated.OwnerId,
                    updated,
                    NotificationHubEvents.PoiUpdated,
                    updated.IsApproved ? "updated" : "pending_review",
                    "POI đã cập nhật",
                    $"POI \"{updated.Name}\" vừa được cập nhật.",
                    actorName);
            }

            return NoContent();
        }

        /// <summary>
        /// POI Owner/Admin: Xóa địa điểm
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            if (!User.IsAdminRole() && !User.IsPoiOwnerRole()) return Forbid();
            var existing = await _locationService.GetLocationByIdAsync(id);
            if (existing == null) return NotFound();

            // POI Owner chỉ được xóa địa điểm của mình
            if (User.IsPoiOwnerRole())
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
            return Ok(locations.Select(location => MapToDto(location, ResolveLanguage())));
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
            return Ok(locations.Select(location => MapToDto(location, ResolveLanguage())));
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

            if (location.IsApproved)
                return BadRequest(new { message = "Địa điểm đã được duyệt trước đó" });

            // FIX: dùng ApproveLocationAsync thay vì UpdateLocationAsync
            // vì UpdateLocationAsync không persist IsApproved và ApprovedAt
            var approved = await _locationService.ApproveLocationAsync(id);
            if (!approved) return NotFound();

            // Đẩy thẳng vào AudioTaskManager để tránh queue trung gian dư thừa.
            _audioTaskManager.EnqueueTask(id, location.Name);
            var actorName = GetActorDisplayName("Admin");

            // Gửi thông báo cho POI Owner (chủ POI)
            if (!string.IsNullOrEmpty(location.OwnerId))
            {
                var notification = await CreateNotificationAsync(
                    targetUserId: location.OwnerId,
                    title: "POI đã được duyệt ✅",
                    message: $"POI \"{location.Name}\" đã được Admin duyệt và hiển thị công khai.",
                    type: NotificationType.POI_Approved,
                    relatedId: id,
                    senderName: actorName);

                await SendNotificationToUserAsync(notification, location.OwnerId);
                await PublishPoiRealtimeToUserAsync(
                    location.OwnerId,
                    location,
                    NotificationHubEvents.ModerationChanged,
                    "approved",
                    "POI đã được duyệt",
                    $"POI \"{location.Name}\" đã được duyệt và sẵn sàng phát hành.",
                    actorName);
            }

            await PublishPoiRealtimeToRoleAsync(
                AppRoles.Admin,
                NotificationHubEvents.ModerationChanged,
                location,
                "approved",
                "POI đã duyệt",
                $"POI \"{location.Name}\" đã được duyệt.",
                actorName);
            await PublishPoiRealtimeToPoiAsync(
                location,
                NotificationHubEvents.PoiUpdated,
                "approved",
                "POI công khai",
                $"POI \"{location.Name}\" đã được công khai.",
                actorName);

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
            var actorName = GetActorDisplayName("Admin");

            // Gửi thông báo cho POI Owner trước khi xóa
            if (!string.IsNullOrEmpty(location.OwnerId))
            {
                var notification = await CreateNotificationAsync(
                    targetUserId: location.OwnerId,
                    title: "POI bị từ chối ❌",
                    message: $"POI \"{location.Name}\" đã bị Admin từ chối.",
                    type: NotificationType.POI_Rejected,
                    relatedId: id,
                    senderName: actorName);

                await SendNotificationToUserAsync(notification, location.OwnerId);
                await PublishPoiRealtimeToUserAsync(
                    location.OwnerId,
                    location,
                    NotificationHubEvents.ModerationChanged,
                    "rejected",
                    "POI bị từ chối",
                    $"POI \"{location.Name}\" đã bị từ chối.",
                    actorName);
            }

            await PublishPoiRealtimeToRoleAsync(
                AppRoles.Admin,
                NotificationHubEvents.ModerationChanged,
                location,
                "rejected",
                "POI bị từ chối",
                $"POI \"{location.Name}\" đã bị từ chối khỏi hệ thống.",
                actorName);

            await _locationService.DeleteLocationAsync(id);

            return Ok(new { message = "Địa điểm đã bị từ chối và xóa", locationId = id });
        }

        // ========================================
        // Helper: Map Location entity to DTO
        // ========================================
        private string ResolveLanguage()
        {
            return PoiContentResolver.NormalizeLanguageCode(Request.Headers["Accept-Language"].ToString());
        }

        private string GetActorDisplayName(string fallback)
        {
            return User.Claims.FirstOrDefault(c =>
                       c.Type == "name" || c.Type == System.Security.Claims.ClaimTypes.Name)?.Value
                   ?? User.Identity?.Name
                   ?? fallback;
        }

        private async Task<Notification> CreateNotificationAsync(
            string? targetRole = null,
            string? targetUserId = null,
            string title = "",
            string message = "",
            NotificationType type = NotificationType.System,
            int? relatedId = null,
            string? senderName = null)
        {
            var notification = new Notification
            {
                TargetRole = targetRole,
                UserId = targetUserId,
                Title = title,
                Message = message,
                Type = type,
                RelatedId = relatedId,
                SenderName = senderName
            };

            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();
            return notification;
        }

        private Task SendNotificationToRoleAsync(Notification notification, string role, CancellationToken cancellationToken = default)
        {
            return _hubContext.Clients.Group(NotificationHubGroups.Role(role)).SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                Type = notification.Type.ToString(),
                notification.CreatedAt,
                notification.RelatedId,
                notification.SenderName
            }, cancellationToken);
        }

        private Task SendNotificationToUserAsync(Notification notification, string userId, CancellationToken cancellationToken = default)
        {
            return _hubContext.Clients.Group(NotificationHubGroups.User(userId)).SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                Type = notification.Type.ToString(),
                notification.CreatedAt,
                notification.RelatedId,
                notification.SenderName
            }, cancellationToken);
        }

        private Task PublishPoiRealtimeToRoleAsync(
            string role,
            string eventName,
            Location location,
            string status,
            string title,
            string message,
            string? triggeredBy = null,
            CancellationToken cancellationToken = default)
        {
            return _hubContext.SendRealtimeToRoleAsync(role, eventName, BuildPoiRealtimeMessage(location, status, title, message, triggeredBy), cancellationToken);
        }

        private Task PublishPoiRealtimeToUserAsync(
            string userId,
            Location location,
            string eventName,
            string status,
            string title,
            string message,
            string? triggeredBy = null,
            CancellationToken cancellationToken = default)
        {
            return _hubContext.SendRealtimeToUserAsync(userId, eventName, BuildPoiRealtimeMessage(location, status, title, message, triggeredBy), cancellationToken);
        }

        private Task PublishPoiRealtimeToPoiAsync(
            Location location,
            string eventName,
            string status,
            string title,
            string message,
            string? triggeredBy = null,
            CancellationToken cancellationToken = default)
        {
            return _hubContext.SendRealtimeToPoiAsync(location.Id, eventName, BuildPoiRealtimeMessage(location, status, title, message, triggeredBy), cancellationToken);
        }

        private static RealtimeActivityMessage BuildPoiRealtimeMessage(Location location, string status, string title, string message, string? triggeredBy)
        {
            return new RealtimeActivityMessage
            {
                EntityType = "poi",
                EntityId = location.Id,
                Status = status,
                Title = title,
                Message = message,
                TriggeredBy = triggeredBy
            };
        }

        private static LocationDto MapToDto(Location l, string languageCode)
        {
            var resolved = PoiContentResolver.Resolve(l, languageCode);

            return new LocationDto
            {
                Id = l.Id,
                Name = resolved.Name,
                Description = resolved.Description,
                Address = l.Address,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                Radius = l.Radius,
                Priority = l.Priority,
                ImageUrl = l.ImageUrl,
                MapLink = l.MapLink,
                TtsScript = resolved.TtsScript,
                CategoryId = l.CategoryId,
                CategoryName = l.Category?.Name,
                IsApproved = l.IsApproved,
                ApprovedAt = l.ApprovedAt,
                OwnerId = l.OwnerId,
                HasAudio = resolved.HasAudio,
                AudioUrl = resolved.AudioUrl,
                AudioStatus = resolved.AudioStatus,
                LanguageCode = resolved.LanguageCode,
                Tier = resolved.Tier,
                FallbackUsed = resolved.FallbackUsed,
                IsFallback = resolved.IsFallback
            };
        }
    }
}
