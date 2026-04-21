using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Extensions;
using NarrationApp.Server.Services;
using NarrationApp.Shared.DTOs.Common;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Route("api/visit-events")]
public sealed class VisitEventsController(IVisitEventService visitEventService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> CreateAsync(VisitEventService.CreateVisitEventRequest request, CancellationToken cancellationToken)
    {
        var finalRequest = new VisitEventService.CreateVisitEventRequest
        {
            UserId = User.Identity?.IsAuthenticated == true ? User.GetRequiredUserId() : request.UserId,
            DeviceId = request.DeviceId,
            PoiId = request.PoiId,
            EventType = request.EventType,
            Source = request.Source,
            ListenDurationSeconds = request.ListenDurationSeconds,
            Lat = request.Lat,
            Lng = request.Lng
        };

        await visitEventService.CreateAsync(finalRequest, cancellationToken);
        return Ok(new ApiResponse<object> { Succeeded = true, Message = "Visit event created." });
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<object>>>> GetByPoiAsync([FromQuery] int poiId, CancellationToken cancellationToken)
    {
        var response = await visitEventService.GetByPoiAsync(poiId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<object>> { Succeeded = true, Message = "Visit events loaded.", Data = response });
    }
}
