using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Extensions;
using NarrationApp.Server.Services;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Notification;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>>> GetAsync(CancellationToken cancellationToken)
    {
        var response = await notificationService.GetByUserAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<NotificationDto>> { Succeeded = true, Message = "Notifications loaded.", Data = response });
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<UnreadCountDto>>> GetUnreadCountAsync(CancellationToken cancellationToken)
    {
        var response = await notificationService.GetUnreadCountAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(new ApiResponse<UnreadCountDto> { Succeeded = true, Message = "Unread count loaded.", Data = response });
    }

    [HttpPut("{id:int}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkReadAsync(int id, CancellationToken cancellationToken)
    {
        await notificationService.MarkReadAsync(User.GetRequiredUserId(), id, cancellationToken);
        return Ok(new ApiResponse<object> { Succeeded = true, Message = "Notification marked as read." });
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllReadAsync(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllReadAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(new ApiResponse<object> { Succeeded = true, Message = "All notifications marked as read." });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await notificationService.DeleteAsync(User.GetRequiredUserId(), id, cancellationToken);
        return Ok(new ApiResponse<object> { Succeeded = true, Message = "Notification deleted." });
    }
}
