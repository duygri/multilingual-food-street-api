using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed partial class AnalyticsService(AppDbContext dbContext) : IAnalyticsService
{
    private static readonly TimeSpan MovementSessionGap = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DefaultAllTimeDecayTau = TimeSpan.FromDays(30);
    private static readonly GaussianKernelEntry[] GaussianKernel = BuildGaussianKernel();
    private const double EarthRadiusMeters = 6378137d;
    private const int GaussianKernelRadius = 2;
    private const double GaussianSigma = 1d;
    private const double MinimumVisibleHeatWeight = 0.05d;
    private const int MinimumAnonymousSessions = 3;
    private const double District4MinimumLatitude = 10.747d;
    private const double District4MaximumLatitude = 10.7735d;
    private const double District4MinimumLongitude = 106.691d;
    private const double District4MaximumLongitude = 106.718d;

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

    public async Task<AnalyticsSnapshotDto> GetAnalyticsSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var currentMonthStartUtc = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var eventCounts = await dbContext.VisitEvents
            .AsNoTracking()
            .GroupBy(item => item.EventType)
            .Select(group => new
            {
                EventType = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        var averageListenDurationSeconds = await dbContext.VisitEvents
            .AsNoTracking()
            .Where(item => item.EventType == EventType.AudioPlay)
            .AverageAsync(item => (double?)item.ListenDurationSeconds, cancellationToken) ?? 0d;

        var currentMonthGeofenceTriggers = await dbContext.VisitEvents
            .AsNoTracking()
            .CountAsync(
                item => item.EventType == EventType.GeofenceEnter
                    && item.CreatedAt >= currentMonthStartUtc,
                cancellationToken);

        return new AnalyticsSnapshotDto
        {
            GeofenceTriggers = eventCounts
                .Where(item => item.EventType == EventType.GeofenceEnter)
                .Sum(item => item.Count),
            CurrentMonthGeofenceTriggers = currentMonthGeofenceTriggers,
            AudioPlays = eventCounts
                .Where(item => item.EventType == EventType.AudioPlay)
                .Sum(item => item.Count),
            QrScans = eventCounts
                .Where(item => item.EventType == EventType.QrScan)
                .Sum(item => item.Count),
            AverageListenDurationSeconds = averageListenDurationSeconds
        };
    }

    public Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default) =>
        GetHeatmapAsync(new HeatmapQueryDto(), cancellationToken);

    public async Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(HeatmapQueryDto query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalizedQuery = NormalizeHeatmapQuery(query);
        var referenceTimeUtc = normalizedQuery.ReferenceTimeUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        var cutoffUtc = ResolveHeatmapCutoff(normalizedQuery.TimeRange, referenceTimeUtc);

        var visitEventsQuery = dbContext.VisitEvents
            .AsNoTracking()
            .Where(item => item.Lat.HasValue && item.Lng.HasValue)
            .Where(item =>
                item.Lat!.Value >= District4MinimumLatitude
                    && item.Lat.Value <= District4MaximumLatitude
                    && item.Lng!.Value >= District4MinimumLongitude
                    && item.Lng.Value <= District4MaximumLongitude)
            .Where(item => !string.IsNullOrWhiteSpace(item.DeviceId));

        if (normalizedQuery.EventTypeFilter.HasValue)
        {
            visitEventsQuery = visitEventsQuery.Where(item => item.EventType == normalizedQuery.EventTypeFilter.Value);
        }

        if (cutoffUtc.HasValue)
        {
            visitEventsQuery = visitEventsQuery.Where(item => item.CreatedAt >= cutoffUtc.Value);
        }

        var events = await visitEventsQuery
            .OrderBy(item => item.DeviceId)
            .ThenBy(item => item.CreatedAt)
            .Select(item => new HeatmapVisitRecord
            {
                DeviceId = item.DeviceId,
                Lat = item.Lat!.Value,
                Lng = item.Lng!.Value,
                CreatedAt = item.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var baseGridWeights = BuildSessionWeightedGrid(events, normalizedQuery, referenceTimeUtc);
        var weightedGrid = normalizedQuery.ApplyGaussianSmoothing
            ? ApplyGaussianSmoothing(baseGridWeights)
            : baseGridWeights;

        return weightedGrid
            .Where(item => item.Value >= MinimumVisibleHeatWeight)
            .OrderByDescending(item => item.Value)
            .Select(item =>
            {
                var coordinate = GetGridCellCenter(item.Key, normalizedQuery.GridSizeMeters);
                return new HeatmapPointDto
                {
                    Lat = coordinate.Lat,
                    Lng = coordinate.Lng,
                    Weight = Math.Min(item.Value, normalizedQuery.MaxWeight)
                };
            })
            .Where(item => IsWithinDistrict4Bounds(item.Lat, item.Lng))
            .ToArray();
    }

    public Task<IReadOnlyList<MovementFlowDto>> GetMovementFlowsAsync(CancellationToken cancellationToken = default) =>
        GetMovementFlowsAsync(new MovementFlowQueryDto(), cancellationToken);

    public async Task<IReadOnlyList<MovementFlowDto>> GetMovementFlowsAsync(MovementFlowQueryDto query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalizedQuery = NormalizeMovementFlowQuery(query);
        var referenceTimeUtc = normalizedQuery.ReferenceTimeUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        var cutoffUtc = ResolveHeatmapCutoff(normalizedQuery.TimeRange, referenceTimeUtc);

        var visitEventsQuery = dbContext.VisitEvents
            .AsNoTracking()
            .Include(item => item.Poi)
            .Where(item => !string.IsNullOrWhiteSpace(item.DeviceId))
            .Where(item => item.Poi != null)
            .Where(item =>
                item.Poi!.Lat >= District4MinimumLatitude
                    && item.Poi.Lat <= District4MaximumLatitude
                    && item.Poi.Lng >= District4MinimumLongitude
                    && item.Poi.Lng <= District4MaximumLongitude);

        if (normalizedQuery.EventTypeFilter.HasValue)
        {
            visitEventsQuery = visitEventsQuery.Where(item => item.EventType == normalizedQuery.EventTypeFilter.Value);
        }

        if (cutoffUtc.HasValue)
        {
            visitEventsQuery = visitEventsQuery.Where(item => item.CreatedAt >= cutoffUtc.Value);
        }

        var events = await visitEventsQuery
            .OrderBy(item => item.DeviceId)
            .ThenBy(item => item.CreatedAt)
            .Select(item => new MovementVisitRecord
            {
                DeviceId = item.DeviceId,
                PoiId = item.PoiId,
                PoiName = item.Poi!.Name,
                PoiLat = item.Poi.Lat,
                PoiLng = item.Poi.Lng,
                CreatedAt = item.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var aggregates = new Dictionary<(int FromPoiId, int ToPoiId), MovementFlowAggregate>();

        foreach (var deviceGroup in events.GroupBy(item => item.DeviceId, StringComparer.Ordinal))
        {
            List<MovementVisitRecord> sessionStops = [];
            DateTime? previousTimestamp = null;

            foreach (var visitEvent in deviceGroup)
            {
                if (previousTimestamp.HasValue && visitEvent.CreatedAt - previousTimestamp.Value > MovementSessionGap)
                {
                    AccumulateSessionFlows(sessionStops, aggregates);
                    sessionStops.Clear();
                }

                if (sessionStops.Count == 0 || sessionStops[^1].PoiId != visitEvent.PoiId)
                {
                    sessionStops.Add(visitEvent);
                }

                previousTimestamp = visitEvent.CreatedAt;
            }

            AccumulateSessionFlows(sessionStops, aggregates);
        }

        return aggregates.Values
            .Where(item => item.UniqueSessions >= normalizedQuery.MinimumUniqueSessions)
            .OrderByDescending(item => item.Weight)
            .ThenByDescending(item => item.UniqueSessions)
            .Select(item => new MovementFlowDto
            {
                FromPoiId = item.FromPoiId,
                FromPoiName = item.FromPoiName,
                FromLat = item.FromLat,
                FromLng = item.FromLng,
                ToPoiId = item.ToPoiId,
                ToPoiName = item.ToPoiName,
                ToLat = item.ToLat,
                ToLng = item.ToLng,
                Weight = item.Weight,
                UniqueSessions = item.UniqueSessions
            })
            .ToArray();
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

    public async Task<IReadOnlyList<PoiAverageListenDto>> GetAverageListenByPoiAsync(int take = 10, CancellationToken cancellationToken = default)
    {
        return await dbContext.VisitEvents
            .AsNoTracking()
            .Where(item => item.EventType == EventType.AudioPlay)
            .GroupBy(item => new { item.PoiId, item.Poi!.Name })
            .OrderByDescending(group => group.Average(item => item.ListenDurationSeconds))
            .ThenByDescending(group => group.Count())
            .Take(take)
            .Select(group => new PoiAverageListenDto
            {
                PoiId = group.Key.PoiId,
                PoiName = group.Key.Name,
                AverageListenDurationSeconds = group.Average(item => item.ListenDurationSeconds),
                AudioPlayCount = group.Count()
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
