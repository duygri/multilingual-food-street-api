using Microsoft.AspNetCore.SignalR;
using NarrationApp.Server.Hubs;
using NarrationApp.Shared.DTOs.Notification;

namespace NarrationApp.Server.Services;

public sealed class SignalRNotificationBroadcaster(IHubContext<NotificationHub> hubContext) : INotificationBroadcaster
{
    public Task BroadcastAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients.User(userId.ToString()).SendCoreAsync("notificationReceived", [notification], cancellationToken);
    }
}
