using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NarrationApp.Server.Extensions;
using NarrationApp.Server.Services;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Common;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Route("api/audio")]
public sealed class AudioController(IAudioService audioService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AudioDto>>>> GetByPoiAsync([FromQuery] int poiId, [FromQuery] string? lang, CancellationToken cancellationToken)
    {
        var response = await audioService.GetByPoiAsync(poiId, lang, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<AudioDto>> { Succeeded = true, Message = "Audio assets loaded.", Data = response });
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/stream")]
    public async Task<IActionResult> StreamAsync(int id, CancellationToken cancellationToken)
    {
        var stream = await audioService.OpenReadStreamAsync(id, cancellationToken);
        return File(stream, "audio/mpeg", enableRangeProcessing: true);
    }

    [Authorize(Roles = "poi_owner,admin")]
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<AudioDto>>> UploadAsync([FromForm] UploadAudioFormRequest request, CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();
        var response = await audioService.UploadAsync(
            User.GetRequiredUserId(),
            User.GetRequiredUserRole(),
            new UploadAudioRequest
            {
                PoiId = request.PoiId,
                LanguageCode = request.LanguageCode,
                FileName = request.File.FileName
            },
            stream,
            cancellationToken);

        return Ok(new ApiResponse<AudioDto> { Succeeded = true, Message = "Audio uploaded.", Data = response });
    }

    [Authorize(Roles = "admin")]
    [EnableRateLimiting(AppConstants.GenerationRateLimitPolicyName)]
    [HttpPost("tts")]
    public async Task<ActionResult<ApiResponse<AudioDto>>> GenerateTtsAsync(TtsGenerateRequest request, CancellationToken cancellationToken)
    {
        var response = await audioService.GenerateTtsAsync(User.GetRequiredUserId(), User.GetRequiredUserRole(), request, cancellationToken);
        return Ok(new ApiResponse<AudioDto> { Succeeded = true, Message = "TTS audio generated.", Data = response });
    }

    [Authorize(Roles = "admin")]
    [EnableRateLimiting(AppConstants.GenerationRateLimitPolicyName)]
    [HttpPost("generate-from-translation")]
    public async Task<ActionResult<ApiResponse<AudioDto>>> GenerateFromTranslationAsync(GenerateAudioFromTranslationRequest request, CancellationToken cancellationToken)
    {
        var response = await audioService.GenerateFromTranslationAsync(User.GetRequiredUserId(), User.GetRequiredUserRole(), request, cancellationToken);
        return Ok(new ApiResponse<AudioDto> { Succeeded = true, Message = "Audio generated from saved translation.", Data = response });
    }

    [Authorize(Roles = "admin")]
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<AudioDto>>> UpdateAsync(int id, UpdateAudioRequest request, CancellationToken cancellationToken)
    {
        var response = await audioService.UpdateAsync(User.GetRequiredUserId(), User.GetRequiredUserRole(), id, request, cancellationToken);
        return Ok(new ApiResponse<AudioDto> { Succeeded = true, Message = "Audio asset updated.", Data = response });
    }

    [Authorize(Roles = "admin")]
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await audioService.DeleteAsync(User.GetRequiredUserId(), User.GetRequiredUserRole(), id, cancellationToken);
        return Ok(new ApiResponse<object> { Succeeded = true, Message = "Audio asset deleted." });
    }

    public sealed class UploadAudioFormRequest
    {
        public int PoiId { get; init; }

        public string LanguageCode { get; init; } = string.Empty;

        public IFormFile File { get; init; } = default!;
    }
}
