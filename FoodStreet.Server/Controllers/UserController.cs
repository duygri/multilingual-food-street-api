using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodStreet.Server.Constants;
using PROJECT_C_.DTOs;
using FoodStreet.Server.Extensions;
using Microsoft.AspNetCore.SignalR;
using PROJECT_C_.Data;
using PROJECT_C_.Models;
using FoodStreet.Server.Hubs;

namespace PROJECT_C_.Controllers
{
    [Route("api/admin/users")]
    [Route("api/user")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public UserController(
            UserManager<IdentityUser> userManager,
            AppDbContext context,
            IHubContext<NotificationHub> hubContext)
        {
            _userManager = userManager;
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            if (!User.IsAdminRole()) return Forbid();
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    Roles = roles.ToList(),
                    IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow
                });
            }

            return Ok(userDtos);
        }

        /// <summary>
        /// Admin tạo user mới (gán role Admin hoặc POI Owner)
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!User.IsAdminRole()) return Forbid();
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Email và mật khẩu không được để trống" });

            request.Role = AppRoles.NormalizeForPersistence(request.Role);
            if (request.Role == AppRoles.Tourist)
            {
                return BadRequest(new { message = "Role không hợp lệ. Chỉ chấp nhận: Admin, POI Owner" });
            }

            var user = new IdentityUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Tạo tài khoản thất bại",
                    errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            await _userManager.AddToRoleAsync(user, request.Role);

            return Ok(new { message = $"Tạo tài khoản {AppRoles.ToDisplayName(request.Role)} thành công" });
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApprovePoiOwner(string id)
        {
            if (!User.IsAdminRole()) return Forbid();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, AppRoles.PoiOwner))
                return BadRequest(new { message = "User is not a POI Owner" });

            if (user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow)
                return BadRequest(new { message = "POI Owner is already active" });

            user.LockoutEnd = null;
            await _userManager.UpdateAsync(user);

            var actorName = User.Identity?.Name ?? "Admin";
            var notification = new Notification
            {
                UserId = user.Id,
                Title = "Tài khoản POI Owner đã được duyệt ✅",
                Message = "Tài khoản của bạn đã được kích hoạt. Bạn có thể đăng nhập và quản lý nội dung POI.",
                Type = NotificationType.System,
                SenderName = actorName
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(NotificationHubGroups.User(user.Id)).SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                Type = notification.Type.ToString(),
                notification.CreatedAt,
                notification.RelatedId,
                notification.SenderName
            });

            await _hubContext.SendRealtimeToUserAsync(
                user.Id,
                NotificationHubEvents.ModerationChanged,
                new RealtimeActivityMessage
                {
                    EntityType = "owner_account",
                    EntityId = null,
                    Status = "approved",
                    Title = "Tài khoản POI Owner đã được duyệt",
                    Message = $"Tài khoản {user.Email} đã được kích hoạt.",
                    TriggeredBy = actorName
                });

            return Ok(new { message = "POI Owner approved successfully" });
        }

        [HttpPost("{id}/toggle-lock")]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Cannot lock Admin to prevent lockout
            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
                return BadRequest("Cannot lock Admin account");

            bool wasLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

            if (wasLocked)
            {
                user.LockoutEnd = null; // Unlock
            }
            else
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100); // Lock
            }

            await _userManager.UpdateAsync(user);
            // isLocked = !wasLocked because we toggled the state above
            return Ok(new { message = "Lock status updated", isLocked = !wasLocked });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
                return BadRequest("Cannot delete Admin account");

            await _userManager.DeleteAsync(user);
            return NoContent();
        }
    }

}
// NOTE: UserDto and CreateUserRequest have been moved to their own files:
// DTOs/UserDto.cs and DTOs/CreateUserRequest.cs

