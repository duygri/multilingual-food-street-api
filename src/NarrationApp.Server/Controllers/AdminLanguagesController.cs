using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NarrationApp.Server.Services;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Languages;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/languages")]
public sealed class AdminLanguagesController(IManagedLanguageService managedLanguageService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ManagedLanguageDto>>>> GetAsync(CancellationToken cancellationToken)
    {
        var response = await managedLanguageService.GetAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<ManagedLanguageDto>> { Succeeded = true, Message = "Managed languages loaded.", Data = response });
    }

    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ManagedLanguageDto>>> CreateAsync(CreateManagedLanguageRequest request, CancellationToken cancellationToken)
    {
        var response = await managedLanguageService.CreateAsync(request, cancellationToken);
        return Ok(new ApiResponse<ManagedLanguageDto> { Succeeded = true, Message = "Managed language saved.", Data = response });
    }

    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpDelete("{code}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(string code, CancellationToken cancellationToken)
    {
        await managedLanguageService.DeleteAsync(code, cancellationToken);
        return Ok(new ApiResponse<object> { Succeeded = true, Message = "Managed language deleted." });
    }
}
