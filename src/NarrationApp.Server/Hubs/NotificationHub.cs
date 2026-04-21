using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NarrationApp.Server.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
}
