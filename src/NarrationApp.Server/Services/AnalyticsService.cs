using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class AnalyticsService(AppDbContext dbContext) : IAnalyticsService
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        return new DashboardDto
        {
            TotalPois = await dbContext.Pois.CountAsync(cancellationToken),
            PublishedPois = await dbContext.Pois.CountAsync(item => item.Status == PoiStatus.Published, cancellationToken),
            TotalTours = await dbContext.Tours.CountAsync(cancellationToken),
            TotalAudioAssets = await dbContext.AudioAssets.CountAsync(cancellationToken),
            PendingModerationRequests = await dbContext.ModerationRequests.CountAsync(item => item.Status == ModerationStatus.Pending, cancellationToken),
            UnreadNotifications = await dbContext.Notifications.CountAsync(item => !item.IsRead, cancellationToken),
            TopPois = (await GetTopPoisAsync(5, cancellationToken)).ToArray()
        };
    }

    public async Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default)
    {
        var items = await dbContext.VisitEvents
            .AsNoTracking()
            .Where(item => item.Lat.HasValue && item.Lng.HasValue)
            .GroupBy(item => new { item.Lat, item.Lng })
            .Select(group => new HeatmapPointDto
            {
                Lat = group.Key.Lat!.Value,
                Lng = group.Key.Lng!.Value,
                Weight = group.Count()
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<IReadOnlyList<TopPoiDto>> GetTopPoisAsync(int take = 10, CancellationToken cancellationToken = default)
    {
        return await dbContext.VisitEvents
            .AsNoTracking()
            .GroupBy(item => new { item.PoiId, item.Poi!.Name })
            .OrderByDescending(group => group.Count())
            .Take(take)
            .Select(group => new TopPoiDto
            {
                PoiId = group.Key.PoiId,
                PoiName = group.Key.Name,
                Visits = group.Count()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PoiAnalyticsDto> GetPoiAnalyticsAsync(int poiId, CancellationToken cancellationToken = default)
    {
        var totalVisits = await dbContext.VisitEvents.CountAsync(item => item.PoiId == poiId, cancellationToken);
        var audioPlays = await dbContext.VisitEvents.CountAsync(item => item.PoiId == poiId && item.EventType == EventType.AudioPlay, cancellationToken);

        return new PoiAnalyticsDto
        {
            PoiId = poiId,
            TotalVisits = totalVisits,
            AudioPlays = audioPlays
        };
    }

    public async Task<AudioPlayAnalyticsDto> GetAudioPlayAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var totalAudioPlays = await dbContext.VisitEvents.CountAsync(item => item.EventType == EventType.AudioPlay, cancellationToken);
        var totalListenSeconds = await dbContext.VisitEvents
            .Where(item => item.EventType == EventType.AudioPlay)
            .SumAsync(item => (int?)item.ListenDurationSeconds, cancellationToken) ?? 0;

        return new AudioPlayAnalyticsDto
        {
            TotalAudioPlays = totalAudioPlays,
            TotalListenSeconds = totalListenSeconds
        };
    }
}
