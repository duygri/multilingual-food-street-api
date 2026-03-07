using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FoodStreet.Server.Hubs
{
    /// <summary>
    /// SignalR Hub cho hệ thống thông báo real-time
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        /// <summary>
        /// Khi client kết nối → join group theo userId và role
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                // Group theo userId (thông báo cá nhân)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            // Group theo role (thông báo theo vai trò)
            var role = GetRole();
            if (!string.IsNullOrEmpty(role))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{role}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            var role = GetRole();
            if (!string.IsNullOrEmpty(role))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role_{role}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        private string? GetUserId()
        {
            return Context.User?.Claims.FirstOrDefault(c =>
                c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        private string? GetRole()
        {
            return Context.User?.Claims.FirstOrDefault(c =>
                c.Type == "role" || c.Type == ClaimTypes.Role)?.Value;
        }
    }
}
