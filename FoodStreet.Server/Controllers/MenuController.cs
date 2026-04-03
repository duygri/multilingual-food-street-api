using FoodStreet.Server.Constants;
using FoodStreet.Server.Extensions;
using FoodStreet.Server.Hubs;
using FoodStreet.Server.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.DTOs;
using PROJECT_C_.Models;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api")]
    public class MenuController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<NotificationHub> _hubContext;

        public MenuController(AppDbContext db, IHubContext<NotificationHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }

        [HttpGet("content/menu/poi/{locationId:int}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetPublicMenu(int locationId, [FromQuery] string? lang = null)
        {
            var poiExists = await _db.Locations.AnyAsync(location => location.Id == locationId && location.IsApproved);
            if (!poiExists)
            {
                return NotFound(new { message = "POI không tồn tại hoặc chưa được duyệt." });
            }

            var resolvedLanguage = string.IsNullOrWhiteSpace(lang)
                ? Request.Headers["Accept-Language"].ToString()
                : lang;

            var items = await BuildQuery()
                .Where(item => item.LocationId == locationId && item.IsAvailable)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Name)
                .ToListAsync();

            return Ok(items.Select(item => MenuContentResolver.MapResolved(item, resolvedLanguage)));
        }

        [HttpGet("owner/menu")]
        [HttpGet("owner/foods")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetOwnerMenu([FromQuery] int? locationId = null)
        {
            if (!User.IsPoiOwnerRole())
            {
                return Forbid();
            }

            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var query = BuildQuery().Where(item => item.Location.OwnerId == userId);
            if (locationId.HasValue && locationId.Value > 0)
            {
                query = query.Where(item => item.LocationId == locationId.Value);
            }

            var items = await query
                .OrderBy(item => item.Location.Name)
                .ThenBy(item => item.SortOrder)
                .ThenBy(item => item.Name)
                .ToListAsync();

            return Ok(items.Select(MapMenuItem));
        }

        [HttpPost("owner/menu")]
        [HttpPost("owner/foods")]
        [Authorize]
        public async Task<ActionResult<MenuItemDto>> CreateOwnerMenuItem([FromBody] UpsertMenuItemRequest request)
        {
            if (!User.IsPoiOwnerRole())
            {
                return Forbid();
            }

            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var location = await _db.Locations.FirstOrDefaultAsync(item => item.Id == request.LocationId && item.OwnerId == userId);
            if (location == null)
            {
                return NotFound(new { message = "POI không tồn tại hoặc không thuộc owner hiện tại." });
            }

            var menuItem = CreateMenuItemEntity(request, request.LocationId);
            _db.PoiMenuItems.Add(menuItem);
            await _db.SaveChangesAsync();

            menuItem.Location = location;
            var actorName = GetActorDisplayName("POI Owner");
            await NotifyMenuChangedAsync(
                menuItem,
                location,
                notificationTargetRole: AppRoles.Admin,
                notificationTitle: "POI Owner vừa cập nhật menu",
                notificationMessage: $"{actorName} vừa thêm món \"{menuItem.Name}\" vào menu của POI \"{location.Name}\".",
                notificationType: NotificationType.Menu_Created,
                realtimeStatus: "created",
                realtimeTitle: "Menu vừa được thêm",
                realtimeMessage: $"Món \"{menuItem.Name}\" vừa được thêm vào menu của POI \"{location.Name}\".",
                triggeredBy: actorName);

            return CreatedAtAction(nameof(GetOwnerMenu), new { locationId = menuItem.LocationId }, MapMenuItem(menuItem));
        }

        [HttpPut("owner/menu/{id:int}")]
        [HttpPut("owner/foods/{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateOwnerMenuItem(int id, [FromBody] UpsertMenuItemRequest request)
        {
            if (!User.IsPoiOwnerRole())
            {
                return Forbid();
            }

            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var menuItem = await _db.PoiMenuItems
                .Include(item => item.Location)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (menuItem == null)
            {
                return NotFound();
            }

            if (menuItem.Location.OwnerId != userId)
            {
                return Forbid();
            }

            if (menuItem.LocationId != request.LocationId)
            {
                var newLocation = await _db.Locations.FirstOrDefaultAsync(location => location.Id == request.LocationId && location.OwnerId == userId);
                if (newLocation == null)
                {
                    return NotFound(new { message = "POI đích không hợp lệ." });
                }

                menuItem.LocationId = newLocation.Id;
                menuItem.Location = newLocation;
            }

            ApplyMenuItemChanges(menuItem, request);
            await _db.SaveChangesAsync();

            var actorName = GetActorDisplayName("POI Owner");
            await NotifyMenuChangedAsync(
                menuItem,
                menuItem.Location,
                notificationTargetRole: AppRoles.Admin,
                notificationTitle: "POI Owner vừa cập nhật menu",
                notificationMessage: $"{actorName} vừa cập nhật món \"{menuItem.Name}\" tại POI \"{menuItem.Location.Name}\".",
                notificationType: NotificationType.Menu_Created,
                realtimeStatus: "updated",
                realtimeTitle: "Menu vừa được cập nhật",
                realtimeMessage: $"Món \"{menuItem.Name}\" vừa được cập nhật tại POI \"{menuItem.Location.Name}\".",
                triggeredBy: actorName);

            return NoContent();
        }

        [HttpDelete("owner/menu/{id:int}")]
        [HttpDelete("owner/foods/{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteOwnerMenuItem(int id)
        {
            if (!User.IsPoiOwnerRole())
            {
                return Forbid();
            }

            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var menuItem = await _db.PoiMenuItems
                .Include(item => item.Location)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (menuItem == null)
            {
                return NotFound();
            }

            if (menuItem.Location.OwnerId != userId)
            {
                return Forbid();
            }

            _db.PoiMenuItems.Remove(menuItem);
            await _db.SaveChangesAsync();

            var actorName = GetActorDisplayName("POI Owner");
            await NotifyMenuChangedAsync(
                menuItem,
                menuItem.Location,
                notificationTargetRole: AppRoles.Admin,
                notificationTitle: "POI Owner vừa cập nhật menu",
                notificationMessage: $"{actorName} vừa xóa món \"{menuItem.Name}\" khỏi menu của POI \"{menuItem.Location.Name}\".",
                notificationType: NotificationType.Menu_Created,
                realtimeStatus: "deleted",
                realtimeTitle: "Menu vừa được cập nhật",
                realtimeMessage: $"Món \"{menuItem.Name}\" vừa bị xóa khỏi menu của POI \"{menuItem.Location.Name}\".",
                triggeredBy: actorName);

            return NoContent();
        }

        [HttpGet("admin/menu")]
        [HttpGet("admin/foods")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetAdminMenu([FromQuery] int? locationId = null, [FromQuery] bool includeUnavailable = true)
        {
            var query = BuildQuery();
            if (locationId.HasValue && locationId.Value > 0)
            {
                query = query.Where(item => item.LocationId == locationId.Value);
            }

            if (!includeUnavailable)
            {
                query = query.Where(item => item.IsAvailable);
            }

            var items = await query
                .OrderBy(item => item.Location.Name)
                .ThenBy(item => item.SortOrder)
                .ThenBy(item => item.Name)
                .ToListAsync();

            return Ok(items.Select(MapMenuItem));
        }

        [HttpPost("admin/menu")]
        [HttpPost("admin/foods")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<MenuItemDto>> CreateAdminMenuItem([FromBody] UpsertMenuItemRequest request)
        {
            var location = await _db.Locations.FirstOrDefaultAsync(item => item.Id == request.LocationId);
            if (location == null)
            {
                return NotFound(new { message = "POI không tồn tại." });
            }

            var menuItem = CreateMenuItemEntity(request, request.LocationId);
            _db.PoiMenuItems.Add(menuItem);
            await _db.SaveChangesAsync();

            menuItem.Location = location;
            var actorName = GetActorDisplayName("Admin");
            await NotifyMenuChangedAsync(
                menuItem,
                location,
                notificationTargetUserId: location.OwnerId,
                notificationTitle: "Quản trị vừa cập nhật menu",
                notificationMessage: $"{actorName} vừa thêm món \"{menuItem.Name}\" vào menu của POI \"{location.Name}\".",
                notificationType: NotificationType.Menu_Approved,
                realtimeStatus: "created",
                realtimeTitle: "Menu vừa được quản trị cập nhật",
                realtimeMessage: $"Món \"{menuItem.Name}\" vừa được quản trị thêm vào menu của POI \"{location.Name}\".",
                triggeredBy: actorName);

            return CreatedAtAction(nameof(GetAdminMenu), new { locationId = menuItem.LocationId }, MapMenuItem(menuItem));
        }

        [HttpPut("admin/menu/{id:int}")]
        [HttpPut("admin/foods/{id:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> UpdateAdminMenuItem(int id, [FromBody] UpsertMenuItemRequest request)
        {
            var menuItem = await _db.PoiMenuItems
                .Include(item => item.Location)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (menuItem == null)
            {
                return NotFound();
            }

            if (menuItem.LocationId != request.LocationId)
            {
                var newLocation = await _db.Locations.FirstOrDefaultAsync(location => location.Id == request.LocationId);
                if (newLocation == null)
                {
                    return NotFound(new { message = "POI đích không tồn tại." });
                }

                menuItem.LocationId = newLocation.Id;
                menuItem.Location = newLocation;
            }

            ApplyMenuItemChanges(menuItem, request);
            await _db.SaveChangesAsync();

            var actorName = GetActorDisplayName("Admin");
            await NotifyMenuChangedAsync(
                menuItem,
                menuItem.Location,
                notificationTargetUserId: menuItem.Location.OwnerId,
                notificationTitle: "Quản trị vừa cập nhật menu",
                notificationMessage: $"{actorName} vừa cập nhật món \"{menuItem.Name}\" tại POI \"{menuItem.Location.Name}\".",
                notificationType: NotificationType.Menu_Approved,
                realtimeStatus: "updated",
                realtimeTitle: "Menu vừa được quản trị cập nhật",
                realtimeMessage: $"Món \"{menuItem.Name}\" vừa được quản trị cập nhật tại POI \"{menuItem.Location.Name}\".",
                triggeredBy: actorName);

            return NoContent();
        }

        [HttpDelete("admin/menu/{id:int}")]
        [HttpDelete("admin/foods/{id:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> DeleteAdminMenuItem(int id)
        {
            var menuItem = await _db.PoiMenuItems
                .Include(item => item.Location)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (menuItem == null)
            {
                return NotFound();
            }

            _db.PoiMenuItems.Remove(menuItem);
            await _db.SaveChangesAsync();

            var actorName = GetActorDisplayName("Admin");
            await NotifyMenuChangedAsync(
                menuItem,
                menuItem.Location,
                notificationTargetUserId: menuItem.Location.OwnerId,
                notificationTitle: "Quản trị vừa cập nhật menu",
                notificationMessage: $"{actorName} vừa xóa món \"{menuItem.Name}\" khỏi POI \"{menuItem.Location.Name}\".",
                notificationType: NotificationType.Menu_Approved,
                realtimeStatus: "deleted",
                realtimeTitle: "Menu vừa được quản trị cập nhật",
                realtimeMessage: $"Món \"{menuItem.Name}\" vừa được quản trị xóa khỏi POI \"{menuItem.Location.Name}\".",
                triggeredBy: actorName);

            return NoContent();
        }

        private IQueryable<PoiMenuItem> BuildQuery()
        {
            return _db.PoiMenuItems
                .AsNoTracking()
                .Include(item => item.Location)
                .Include(item => item.Translations);
        }

        private static MenuItemDto MapMenuItem(PoiMenuItem item)
        {
            return new MenuItemDto
            {
                Id = item.Id,
                LocationId = item.LocationId,
                LocationName = item.Location?.Name ?? string.Empty,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                Currency = item.Currency,
                ImageUrl = item.ImageUrl,
                IsAvailable = item.IsAvailable,
                SortOrder = item.SortOrder,
                UpdatedAt = item.UpdatedAt,
                TranslationCount = item.Translations?.Count ?? 0
            };
        }

        private static PoiMenuItem CreateMenuItemEntity(UpsertMenuItemRequest request, int locationId)
        {
            return new PoiMenuItem
            {
                LocationId = locationId,
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                Price = request.Price,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "VND" : request.Currency.Trim().ToUpperInvariant(),
                ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
                IsAvailable = request.IsAvailable,
                SortOrder = request.SortOrder,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static void ApplyMenuItemChanges(PoiMenuItem menuItem, UpsertMenuItemRequest request)
        {
            menuItem.Name = request.Name.Trim();
            menuItem.Description = request.Description.Trim();
            menuItem.Price = request.Price;
            menuItem.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "VND" : request.Currency.Trim().ToUpperInvariant();
            menuItem.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
            menuItem.IsAvailable = request.IsAvailable;
            menuItem.SortOrder = request.SortOrder;
            menuItem.UpdatedAt = DateTime.UtcNow;
        }

        private string GetActorDisplayName(string fallback)
        {
            return User.Claims.FirstOrDefault(c =>
                       c.Type == "name" || c.Type == System.Security.Claims.ClaimTypes.Name)?.Value
                   ?? User.Identity?.Name
                   ?? fallback;
        }

        private async Task NotifyMenuChangedAsync(
            PoiMenuItem menuItem,
            Location location,
            string? notificationTargetRole = null,
            string? notificationTargetUserId = null,
            string notificationTitle = "",
            string notificationMessage = "",
            NotificationType notificationType = NotificationType.System,
            string realtimeStatus = "updated",
            string realtimeTitle = "",
            string realtimeMessage = "",
            string? triggeredBy = null,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(notificationTargetRole) || !string.IsNullOrWhiteSpace(notificationTargetUserId))
            {
                var notification = await CreateNotificationAsync(
                    targetRole: notificationTargetRole,
                    targetUserId: notificationTargetUserId,
                    title: notificationTitle,
                    message: notificationMessage,
                    type: notificationType,
                    relatedId: location.Id,
                    senderName: triggeredBy);

                if (!string.IsNullOrWhiteSpace(notificationTargetRole))
                {
                    await SendNotificationToRoleAsync(notification, notificationTargetRole, cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(notificationTargetUserId))
                {
                    await SendNotificationToUserAsync(notification, notificationTargetUserId, cancellationToken);
                }
            }

            var realtimeMessagePayload = BuildMenuRealtimeMessage(menuItem, location, realtimeStatus, realtimeTitle, realtimeMessage, triggeredBy);
            await _hubContext.SendRealtimeToRoleAsync(AppRoles.Admin, NotificationHubEvents.MenuUpdated, realtimeMessagePayload, cancellationToken);

            if (!string.IsNullOrWhiteSpace(location.OwnerId))
            {
                await _hubContext.SendRealtimeToUserAsync(location.OwnerId, NotificationHubEvents.MenuUpdated, BuildMenuRealtimeMessage(menuItem, location, realtimeStatus, realtimeTitle, realtimeMessage, triggeredBy), cancellationToken);
            }

            await _hubContext.SendRealtimeToPoiAsync(location.Id, NotificationHubEvents.MenuUpdated, BuildMenuRealtimeMessage(menuItem, location, realtimeStatus, realtimeTitle, realtimeMessage, triggeredBy), cancellationToken);
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

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
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

        private static RealtimeActivityMessage BuildMenuRealtimeMessage(
            PoiMenuItem menuItem,
            Location location,
            string status,
            string title,
            string message,
            string? triggeredBy)
        {
            return new RealtimeActivityMessage
            {
                EntityType = "menu",
                EntityId = menuItem.Id,
                Status = status,
                Title = title,
                Message = message,
                TriggeredBy = triggeredBy,
                OccurredAt = DateTime.UtcNow
            };
        }
    }
}
