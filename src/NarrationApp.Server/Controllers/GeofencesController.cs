using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Services;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Geofence;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Route("api/geofences")]
public sealed class GeofencesController(IGeofenceService geofenceService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GeofenceDto>>>> GetByPoiAsync([FromQuery] int poiId, CancellationToken cancellationToken)
    {
        var response = await geofenceService.GetByPoiAsync(poiId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<GeofenceDto>> { Succeeded = true, Message = "Geofences loaded.", Data = response });
    }

    [Authorize(Roles = "poi_owner,admin")]
    [HttpPut("{poiId:int}")]
    public async Task<ActionResult<ApiResponse<GeofenceDto>>> UpdateAsync(int poiId, UpdateGeofenceRequest request, CancellationToken cancellationToken)
    {
        var response = await geofenceService.UpdateAsync(poiId, request, cancellationToken);
        return Ok(new ApiResponse<GeofenceDto> { Succeeded = true, Message = "Geofence updated.", Data = response });
    }
}
