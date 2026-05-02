using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Services;
using NarrationApp.Shared.DTOs.Common;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Route("api/visitor-presence")]
public sealed class VisitorPresenceController(IVisitorMobilePresenceTracker visitorMobilePresenceTracker) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("heartbeat")]
    public ActionResult<ApiResponse<object>> Heartbeat([FromBody] VisitorPresenceHeartbeatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return BadRequest(new ApiResponse<object>
            {
                Succeeded = false,
                Message = "DeviceId is required."
            });
        }

        visitorMobilePresenceTracker.Track(
            request.DeviceId,
            request.Source,
            request.PreferredLanguage,
            DateTime.UtcNow);

        return Ok(new ApiResponse<object>
        {
            Succeeded = true,
            Message = "Visitor presence tracked."
        });
    }

    public sealed class VisitorPresenceHeartbeatRequest
    {
        public string DeviceId { get; init; } = string.Empty;

        public string Source { get; init; } = "mobile-presence";

        public string PreferredLanguage { get; init; } = string.Empty;
    }
}
