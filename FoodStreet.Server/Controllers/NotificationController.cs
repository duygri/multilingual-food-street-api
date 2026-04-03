using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using FoodStreet.Server.Constants;
using Microsoft.EntityFrameworkCore;
using FoodStreet.Server.Extensions;
using FoodStreet.Server.Hubs;
using PROJECT_C_.Data;
using PROJECT_C_.Models;

namespace PROJECT_C_.Controllers
{
    [Route("api/content/notifications")]
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationController(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Lấy danh sách thông báo của user hiện tại
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<NotificationDto>>> GetNotifications()
        {
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsAdminRole();
            var role = isAdmin ? AppRoles.Admin : AppRoles.PoiOwner;

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId || n.TargetRole == role)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type.ToString(),
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    RelatedId = n.RelatedId,
                    SenderName = n.SenderName
                })
                .ToListAsync();

            return Ok(notifications);
        }

        /// <summary>
        /// Đếm số thông báo chưa đọc
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsAdminRole();
            var role = isAdmin ? AppRoles.Admin : AppRoles.PoiOwner;

            var count = await _context.Notifications
                .CountAsync(n => (n.UserId == userId || n.TargetRole == role) && !n.IsRead);

            return Ok(count);
        }

        /// <summary>
        /// Đánh dấu thông báo đã đọc
        /// </summary>
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Đánh dấu tất cả đã đọc
        /// </summary>
        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsAdminRole();
            var role = isAdmin ? AppRoles.Admin : AppRoles.PoiOwner;

            await _context.Notifications
                .Where(n => (n.UserId == userId || n.TargetRole == role) && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            return Ok();
        }

        /// <summary>
        /// Xóa một thông báo
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized();

            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            // Chỉ cho phép xóa notification thuộc về user hoặc role của mình
            var isAdmin = User.IsAdminRole();
            var role = isAdmin ? AppRoles.Admin : AppRoles.PoiOwner;
            if (notification.UserId != userId && notification.TargetRole != role)
                return Forbid();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Xóa tất cả thông báo đã đọc
        /// </summary>
        [HttpDelete("clear-read")]
        public async Task<IActionResult> ClearReadNotifications()
        {
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsAdminRole();
            var role = isAdmin ? AppRoles.Admin : AppRoles.PoiOwner;

            await _context.Notifications
                .Where(n => (n.UserId == userId || n.TargetRole == role) && n.IsRead)
                .ExecuteDeleteAsync();

            return NoContent();
        }
    }

    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? RelatedId { get; set; }
        public string? SenderName { get; set; }
    }
}
