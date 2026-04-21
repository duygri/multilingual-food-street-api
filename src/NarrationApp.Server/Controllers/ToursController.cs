using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Extensions;
using NarrationApp.Server.Services;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Tour;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Route("api/tours")]
public sealed class ToursController(ITourService tourService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TourDto>>>> GetAsync(CancellationToken cancellationToken)
    {
        var includeUnpublished = User.Identity?.IsAuthenticated == true && User.IsInRole("admin");
        var response = await tourService.GetAsync(includeUnpublished, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<TourDto>> { Succeeded = true, Message = "Tours loaded.", Data = response });
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<TourDto>>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var includeUnpublished = User.Identity?.IsAuthenticated == true && User.IsInRole("admin");
            var response = await tourService.GetByIdAsync(id, includeUnpublished, cancellationToken);
            return Ok(new ApiResponse<TourDto> { Succeeded = true, Message = "Tour loaded.", Data = response });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<TourDto>
            {
                Succeeded = false,
                Message = "Tour was not found.",
                Error = new ErrorResponse { Code = "tour_not_found", Message = ex.Message }
            });
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TourDto>>> CreateAsync(CreateTourRequest request, CancellationToken cancellationToken)
    {
        var response = await tourService.CreateAsync(request, cancellationToken);
        return Ok(new ApiResponse<TourDto> { Succeeded = true, Message = "Tour created.", Data = response });
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<TourDto>>> UpdateAsync(int id, UpdateTourRequest request, CancellationToken cancellationToken)
    {
        var response = await tourService.UpdateAsync(id, request, cancellationToken);
        return Ok(new ApiResponse<TourDto> { Succeeded = true, Message = "Tour updated.", Data = response });
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await tourService.DeleteAsync(id, cancellationToken);
        return Ok(new ApiResponse<object> { Succeeded = true, Message = "Tour deleted." });
    }

    [Authorize(Roles = "tourist")]
    [HttpPost("{id:int}/start")]
    public async Task<ActionResult<ApiResponse<TourSessionDto>>> StartAsync(int id, [FromBody] StartTourSessionRequest? request, CancellationToken cancellationToken)
    {
        var response = await tourService.StartAsync(id, User.GetRequiredUserId(), request?.DeviceId, cancellationToken);
        return Ok(new ApiResponse<TourSessionDto> { Succeeded = true, Message = "Tour session started.", Data = response });
    }

    [Authorize(Roles = "tourist")]
    [HttpPost("{id:int}/resume")]
    public async Task<ActionResult<ApiResponse<TourSessionDto>>> ResumeAsync(int id, CancellationToken cancellationToken)
    {
        var response = await tourService.ResumeAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(new ApiResponse<TourSessionDto> { Succeeded = true, Message = "Tour session resumed.", Data = response });
    }

    [Authorize(Roles = "tourist")]
    [HttpPost("{id:int}/progress")]
    public async Task<ActionResult<ApiResponse<TourSessionDto>>> ProgressAsync(int id, UpdateTourProgressRequest request, CancellationToken cancellationToken)
    {
        var response = await tourService.ProgressAsync(id, User.GetRequiredUserId(), request, cancellationToken);
        return Ok(new ApiResponse<TourSessionDto> { Succeeded = true, Message = "Tour session progress updated.", Data = response });
    }

    [Authorize(Roles = "tourist")]
    [HttpGet("session/latest")]
    public async Task<ActionResult<ApiResponse<TourSessionDto?>>> LatestAsync(CancellationToken cancellationToken)
    {
        var response = await tourService.GetLatestSessionAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(new ApiResponse<TourSessionDto?> { Succeeded = true, Message = "Latest tour session loaded.", Data = response });
    }

    public sealed class StartTourSessionRequest
    {
        public string? DeviceId { get; init; }
    }
}
