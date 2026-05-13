using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Services;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Visitor;

namespace NarrationApp.Server.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/visitor-notifications")]
public sealed class VisitorNotificationsController(IVisitorNotificationService visitorNotificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VisitorNotificationDto>>>> GetAsync(
        [FromQuery] VisitorNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        var response = await visitorNotificationService.GetAsync(query, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<VisitorNotificationDto>>
        {
            Succeeded = true,
            Message = "Visitor notifications loaded.",
            Data = response
        });
    }
}
