using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NarrationApp.Server.Extensions;
using NarrationApp.Server.Services;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Moderation;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Authorize(Roles = "poi_owner,admin")]
[Route("api/moderation-requests")]
public sealed class ModerationRequestsController(IModerationService moderationService) : ControllerBase
{
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ModerationRequestDto>>> CreateAsync(CreateModerationRequest request, CancellationToken cancellationToken)
    {
        var response = await moderationService.CreateAsync(User.GetRequiredUserId(), request, cancellationToken);
        return Ok(new ApiResponse<ModerationRequestDto> { Succeeded = true, Message = "Moderation request created.", Data = response });
    }

    [Authorize(Roles = "poi_owner")]
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ModerationRequestDto>>>> GetMineAsync(CancellationToken cancellationToken)
    {
        var response = await moderationService.GetByRequesterAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<ModerationRequestDto>> { Succeeded = true, Message = "Moderation requests loaded.", Data = response });
    }
}
