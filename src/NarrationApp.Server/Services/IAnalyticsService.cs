using NarrationApp.Shared.DTOs.Analytics;

namespace NarrationApp.Server.Services;

public interface IAnalyticsService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<AnalyticsSnapshotDto> GetAnalyticsSnapshotAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AnalyticsSnapshotDto());

    Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(HeatmapQueryDto query, CancellationToken cancellationToken = default) =>
        GetHeatmapAsync(cancellationToken);

    Task<IReadOnlyList<MovementFlowDto>> GetMovementFlowsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MovementFlowDto>>(Array.Empty<MovementFlowDto>());

    Task<IReadOnlyList<MovementFlowDto>> GetMovementFlowsAsync(MovementFlowQueryDto query, CancellationToken cancellationToken = default) =>
        GetMovementFlowsAsync(cancellationToken);

    Task<IReadOnlyList<TopPoiDto>> GetTopPoisAsync(int take = 10, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PoiAverageListenDto>> GetAverageListenByPoiAsync(int take = 10, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PoiAverageListenDto>>(Array.Empty<PoiAverageListenDto>());

    Task<PoiAnalyticsDto> GetPoiAnalyticsAsync(int poiId, CancellationToken cancellationToken = default);

    Task<AudioPlayAnalyticsDto> GetAudioPlayAnalyticsAsync(CancellationToken cancellationToken = default);
}
