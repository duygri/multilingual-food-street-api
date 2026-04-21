using NarrationApp.Shared.DTOs.Analytics;

namespace NarrationApp.Server.Services;

public interface IAnalyticsService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopPoiDto>> GetTopPoisAsync(int take = 10, CancellationToken cancellationToken = default);

    Task<PoiAnalyticsDto> GetPoiAnalyticsAsync(int poiId, CancellationToken cancellationToken = default);

    Task<AudioPlayAnalyticsDto> GetAudioPlayAnalyticsAsync(CancellationToken cancellationToken = default);
}
