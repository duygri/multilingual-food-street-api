using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NarrationApp.Server.Services;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Translation;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Route("api/translations")]
public sealed class TranslationsController(ITranslationService translationService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TranslationDto>>>> GetByPoiAsync([FromQuery] int poiId, CancellationToken cancellationToken)
    {
        var response = await translationService.GetByPoiAsync(poiId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<TranslationDto>> { Succeeded = true, Message = "Translations loaded.", Data = response });
    }

    [Authorize(Roles = "admin")]
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TranslationDto>>> UpsertAsync(CreateTranslationRequest request, CancellationToken cancellationToken)
    {
        var response = await translationService.UpsertAsync(request, cancellationToken);
        return Ok(new ApiResponse<TranslationDto> { Succeeded = true, Message = "Translation saved.", Data = response });
    }

    [Authorize(Roles = "admin")]
    [EnableRateLimiting(AppConstants.GenerationRateLimitPolicyName)]
    [HttpPost("{poiId:int}/auto")]
    public async Task<ActionResult<ApiResponse<TranslationDto>>> AutoTranslateAsync(int poiId, [FromQuery] string targetLanguage, CancellationToken cancellationToken)
    {
        var response = await translationService.AutoTranslateAsync(poiId, targetLanguage, cancellationToken);
        return Ok(new ApiResponse<TranslationDto> { Succeeded = true, Message = "Translation generated.", Data = response });
    }

    [Authorize(Roles = "admin")]
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpDelete("{translationId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(int translationId, CancellationToken cancellationToken)
    {
        await translationService.DeleteAsync(translationId, cancellationToken);
        return Ok(new ApiResponse<object> { Succeeded = true, Message = "Translation deleted." });
    }
}
