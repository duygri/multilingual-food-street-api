using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Services;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Common;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/analytics")]
public sealed class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> DashboardAsync(CancellationToken cancellationToken)
    {
        var response = await analyticsService.GetDashboardAsync(cancellationToken);
        return Ok(new ApiResponse<DashboardDto> { Succeeded = true, Message = "Dashboard analytics loaded.", Data = response });
    }

    [HttpGet("snapshot")]
    public async Task<ActionResult<ApiResponse<AnalyticsSnapshotDto>>> SnapshotAsync(CancellationToken cancellationToken)
    {
        var response = await analyticsService.GetAnalyticsSnapshotAsync(cancellationToken);
        return Ok(new ApiResponse<AnalyticsSnapshotDto> { Succeeded = true, Message = "Analytics snapshot loaded.", Data = response });
    }

    [HttpGet("poi/{id:int}")]
    public async Task<ActionResult<ApiResponse<PoiAnalyticsDto>>> PoiAsync(int id, CancellationToken cancellationToken)
    {
        var response = await analyticsService.GetPoiAnalyticsAsync(id, cancellationToken);
        return Ok(new ApiResponse<PoiAnalyticsDto> { Succeeded = true, Message = "POI analytics loaded.", Data = response });
    }

    [HttpGet("heatmap")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HeatmapPointDto>>>> HeatmapAsync([FromQuery] HeatmapQueryDto? query, CancellationToken cancellationToken)
    {
        var response = query is null
            ? await analyticsService.GetHeatmapAsync(cancellationToken)
            : await analyticsService.GetHeatmapAsync(query, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<HeatmapPointDto>> { Succeeded = true, Message = "Heatmap loaded.", Data = response });
    }

    [HttpGet("movement-flows")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MovementFlowDto>>>> MovementFlowsAsync([FromQuery] MovementFlowQueryDto? query, CancellationToken cancellationToken)
    {
        var response = query is null
            ? await analyticsService.GetMovementFlowsAsync(cancellationToken)
            : await analyticsService.GetMovementFlowsAsync(query, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<MovementFlowDto>> { Succeeded = true, Message = "Movement flows loaded.", Data = response });
    }

    [HttpGet("top-pois")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopPoiDto>>>> TopPoisAsync(CancellationToken cancellationToken)
    {
        var response = await analyticsService.GetTopPoisAsync(10, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<TopPoiDto>> { Succeeded = true, Message = "Top POIs loaded.", Data = response });
    }

    [HttpGet("average-listen")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PoiAverageListenDto>>>> AverageListenAsync(CancellationToken cancellationToken)
    {
        var response = await analyticsService.GetAverageListenByPoiAsync(10, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<PoiAverageListenDto>> { Succeeded = true, Message = "Average listen analytics loaded.", Data = response });
    }

    [HttpGet("audio-plays")]
    public async Task<ActionResult<ApiResponse<AudioPlayAnalyticsDto>>> AudioPlaysAsync(CancellationToken cancellationToken)
    {
        var response = await analyticsService.GetAudioPlayAnalyticsAsync(cancellationToken);
        return Ok(new ApiResponse<AudioPlayAnalyticsDto> { Succeeded = true, Message = "Audio play analytics loaded.", Data = response });
    }
}
