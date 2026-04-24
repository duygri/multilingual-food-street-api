using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NarrationApp.Server.Extensions;
using NarrationApp.Server.Services;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Route("api/pois")]
public sealed class PoisController(IPoiService poiService) : ControllerBase
{
    private const long MaxRepresentativeImageBytes = 5_000_000;

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PoiDto>>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var response = await poiService.GetAllAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<PoiDto>> { Succeeded = true, Message = "POIs loaded.", Data = response });
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<PoiDto>>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var response = await poiService.GetByIdAsync(id, cancellationToken);
        if (response is null)
        {
            return NotFound(new ApiResponse<PoiDto>
            {
                Succeeded = false,
                Message = "POI not found.",
                Error = new ErrorResponse { Code = "poi_not_found", Message = "POI not found." }
            });
        }

        return Ok(new ApiResponse<PoiDto> { Succeeded = true, Message = "POI loaded.", Data = response });
    }

    [AllowAnonymous]
    [HttpGet("near")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PoiDto>>>> GetNearbyAsync([FromQuery] PoiNearRequest request, CancellationToken cancellationToken)
    {
        var response = await poiService.GetNearbyAsync(request, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<PoiDto>> { Succeeded = true, Message = "Nearby POIs loaded.", Data = response });
    }

    [Authorize(Roles = "poi_owner")]
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PoiDto>>> CreateAsync(CreatePoiRequest request, CancellationToken cancellationToken)
    {
        var response = await poiService.CreateAsync(User.GetRequiredUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = response.Id }, new ApiResponse<PoiDto>
        {
            Succeeded = true,
            Message = "POI created.",
            Data = response
        });
    }

    [Authorize(Roles = "poi_owner,admin")]
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<PoiDto>>> UpdateAsync(int id, UpdatePoiRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await poiService.UpdateAsync(User.GetRequiredUserId(), User.GetRequiredUserRole(), id, request, cancellationToken);
            return Ok(new ApiResponse<PoiDto> { Succeeded = true, Message = "POI updated.", Data = response });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PoiDto>
            {
                Succeeded = false,
                Message = "POI update forbidden.",
                Error = new ErrorResponse { Code = "poi_forbidden", Message = ex.Message }
            });
        }
    }

    [Authorize(Roles = "poi_owner,admin")]
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPost("{id:int}/image")]
    [RequestSizeLimit(MaxRepresentativeImageBytes)]
    public async Task<ActionResult<ApiResponse<PoiDto>>> UploadImageAsync(int id, [FromForm] UploadPoiImageFormRequest request, CancellationToken cancellationToken)
    {
        if (request.File.Length <= 0 || request.File.Length > MaxRepresentativeImageBytes)
        {
            return BadRequest(new ApiResponse<PoiDto>
            {
                Succeeded = false,
                Message = "Representative image is invalid.",
                Error = new ErrorResponse
                {
                    Code = "invalid_image_upload",
                    Message = "Representative image must be greater than 0 bytes and at most 5 MB."
                }
            });
        }

        try
        {
            await using var stream = request.File.OpenReadStream();
            var response = await poiService.UploadImageAsync(
                User.GetRequiredUserId(),
                User.GetRequiredUserRole(),
                id,
                request.File.FileName,
                request.File.ContentType,
                stream,
                cancellationToken);

            return Ok(new ApiResponse<PoiDto> { Succeeded = true, Message = "Representative image uploaded.", Data = response });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PoiDto>
            {
                Succeeded = false,
                Message = "POI image upload forbidden.",
                Error = new ErrorResponse { Code = "poi_forbidden", Message = ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<PoiDto>
            {
                Succeeded = false,
                Message = "Representative image is invalid.",
                Error = new ErrorResponse { Code = "invalid_image_upload", Message = ex.Message }
            });
        }
    }

    [Authorize(Roles = "poi_owner,admin")]
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpDelete("{id:int}/image")]
    public async Task<ActionResult<ApiResponse<PoiDto>>> DeleteImageAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await poiService.DeleteImageAsync(User.GetRequiredUserId(), User.GetRequiredUserRole(), id, cancellationToken);
            return Ok(new ApiResponse<PoiDto> { Succeeded = true, Message = "Representative image removed.", Data = response });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<PoiDto>
            {
                Succeeded = false,
                Message = "POI image delete forbidden.",
                Error = new ErrorResponse { Code = "poi_forbidden", Message = ex.Message }
            });
        }
    }

    [Authorize(Roles = "poi_owner,admin")]
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await poiService.DeleteAsync(User.GetRequiredUserId(), User.GetRequiredUserRole(), id, cancellationToken);
            return Ok(new ApiResponse<object> { Succeeded = true, Message = "POI deleted." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Succeeded = false,
                Message = "POI delete forbidden.",
                Error = new ErrorResponse { Code = "poi_forbidden", Message = ex.Message }
            });
        }
    }

    public sealed class UploadPoiImageFormRequest
    {
        public IFormFile File { get; init; } = default!;
    }
}
