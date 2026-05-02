using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class AnalyticsService(AppDbContext dbContext) : IAnalyticsService
{
    private static readonly TimeSpan MovementSessionGap = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DefaultAllTimeDecayTau = TimeSpan.FromDays(30);
    private static readonly GaussianKernelEntry[] GaussianKernel = BuildGaussianKernel();
    private const double EarthRadiusMeters = 6378137d;
    private const int GaussianKernelRadius = 2;
    private const double GaussianSigma = 1d;
    private const double MinimumVisibleHeatWeight = 0.05d;
    private const int MinimumAnonymousSessions = 3;

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
            .Where(item => item.Poi != null);

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

    private static void AccumulateSessionFlows(
        IReadOnlyList<MovementVisitRecord> sessionStops,
        IDictionary<(int FromPoiId, int ToPoiId), MovementFlowAggregate> aggregates)
    {
        if (sessionStops.Count < 2)
        {
            return;
        }

        HashSet<(int FromPoiId, int ToPoiId)> sessionEdges = [];

        for (var index = 0; index < sessionStops.Count - 1; index++)
        {
            var from = sessionStops[index];
            var to = sessionStops[index + 1];

            if (from.PoiId == to.PoiId)
            {
                continue;
            }

            var edgeKey = (from.PoiId, to.PoiId);
            if (!aggregates.TryGetValue(edgeKey, out var aggregate))
            {
                aggregate = new MovementFlowAggregate
                {
                    FromPoiId = from.PoiId,
                    FromPoiName = from.PoiName,
                    FromLat = from.PoiLat,
                    FromLng = from.PoiLng,
                    ToPoiId = to.PoiId,
                    ToPoiName = to.PoiName,
                    ToLat = to.PoiLat,
                    ToLng = to.PoiLng
                };
                aggregates[edgeKey] = aggregate;
            }

            aggregate.Weight++;
            if (sessionEdges.Add(edgeKey))
            {
                aggregate.UniqueSessions++;
            }
        }
    }

    private static HeatmapQueryDto NormalizeHeatmapQuery(HeatmapQueryDto query)
    {
        var gridSizeMeters = Math.Clamp(query.GridSizeMeters, 10d, 250d);
        var maxWeight = Math.Clamp(query.MaxWeight, 1d, 500d);

        return new HeatmapQueryDto
        {
            TimeRange = query.TimeRange,
            EventTypeFilter = query.EventTypeFilter,
            UseTimeDecay = query.UseTimeDecay,
            GridSizeMeters = gridSizeMeters,
            MaxWeight = maxWeight,
            ApplyGaussianSmoothing = query.ApplyGaussianSmoothing,
            ReferenceTimeUtc = query.ReferenceTimeUtc
        };
    }

    private static MovementFlowQueryDto NormalizeMovementFlowQuery(MovementFlowQueryDto query)
    {
        var minimumUniqueSessions = query.MinimumUniqueSessions <= 0
            ? MinimumAnonymousSessions
            : query.MinimumUniqueSessions;

        return new MovementFlowQueryDto
        {
            TimeRange = query.TimeRange,
            EventTypeFilter = query.EventTypeFilter,
            MinimumUniqueSessions = Math.Clamp(minimumUniqueSessions, 1, 100),
            ReferenceTimeUtc = query.ReferenceTimeUtc
        };
    }

    private static DateTime? ResolveHeatmapCutoff(HeatmapTimeRange timeRange, DateTime referenceTimeUtc) => timeRange switch
    {
        HeatmapTimeRange.Last24Hours => referenceTimeUtc.AddHours(-24),
        HeatmapTimeRange.Last7Days => referenceTimeUtc.AddDays(-7),
        HeatmapTimeRange.Last30Days => referenceTimeUtc.AddDays(-30),
        _ => null
    };

    private static Dictionary<GridCellKey, double> BuildSessionWeightedGrid(
        IReadOnlyList<HeatmapVisitRecord> events,
        HeatmapQueryDto query,
        DateTime referenceTimeUtc)
    {
        var gridWeights = new Dictionary<GridCellKey, double>();

        foreach (var deviceGroup in events.GroupBy(item => item.DeviceId, StringComparer.Ordinal))
        {
            var sessionCells = new Dictionary<GridCellKey, DateTime>();
            DateTime? previousTimestamp = null;

            foreach (var visitEvent in deviceGroup)
            {
                if (previousTimestamp.HasValue && visitEvent.CreatedAt - previousTimestamp.Value > MovementSessionGap)
                {
                    FlushHeatmapSession(sessionCells, gridWeights, query, referenceTimeUtc);
                    sessionCells.Clear();
                }

                var cell = SnapToGrid(visitEvent.Lat, visitEvent.Lng, query.GridSizeMeters);
                if (!sessionCells.TryGetValue(cell, out var latestSeenAt) || visitEvent.CreatedAt > latestSeenAt)
                {
                    sessionCells[cell] = visitEvent.CreatedAt;
                }

                previousTimestamp = visitEvent.CreatedAt;
            }

            FlushHeatmapSession(sessionCells, gridWeights, query, referenceTimeUtc);
        }

        return gridWeights;
    }

    private static void FlushHeatmapSession(
        IReadOnlyDictionary<GridCellKey, DateTime> sessionCells,
        IDictionary<GridCellKey, double> gridWeights,
        HeatmapQueryDto query,
        DateTime referenceTimeUtc)
    {
        foreach (var cell in sessionCells)
        {
            var weight = query.UseTimeDecay
                ? ComputeTimeDecayWeight(cell.Value, referenceTimeUtc, query.TimeRange)
                : 1d;

            if (gridWeights.TryGetValue(cell.Key, out var existingWeight))
            {
                gridWeights[cell.Key] = existingWeight + weight;
            }
            else
            {
                gridWeights[cell.Key] = weight;
            }
        }
    }

    private static double ComputeTimeDecayWeight(DateTime eventTimeUtc, DateTime referenceTimeUtc, HeatmapTimeRange timeRange)
    {
        var age = referenceTimeUtc - eventTimeUtc.ToUniversalTime();
        if (age <= TimeSpan.Zero)
        {
            return 1d;
        }

        var tau = ResolveDecayTau(timeRange);
        return Math.Exp(-age.TotalSeconds / tau.TotalSeconds);
    }

    private static TimeSpan ResolveDecayTau(HeatmapTimeRange timeRange) => timeRange switch
    {
        HeatmapTimeRange.Last24Hours => TimeSpan.FromHours(12),
        HeatmapTimeRange.Last7Days => TimeSpan.FromDays(3.5),
        HeatmapTimeRange.Last30Days => TimeSpan.FromDays(15),
        _ => DefaultAllTimeDecayTau
    };

    private static Dictionary<GridCellKey, double> ApplyGaussianSmoothing(IReadOnlyDictionary<GridCellKey, double> sourceWeights)
    {
        var smoothed = new Dictionary<GridCellKey, double>();

        foreach (var sourceCell in sourceWeights)
        {
            foreach (var entry in GaussianKernel)
            {
                var targetCell = new GridCellKey(
                    sourceCell.Key.X + entry.OffsetX,
                    sourceCell.Key.Y + entry.OffsetY);
                var contribution = sourceCell.Value * entry.Weight;

                if (smoothed.TryGetValue(targetCell, out var existingWeight))
                {
                    smoothed[targetCell] = existingWeight + contribution;
                }
                else
                {
                    smoothed[targetCell] = contribution;
                }
            }
        }

        return smoothed;
    }

    private static GaussianKernelEntry[] BuildGaussianKernel()
    {
        var kernelWidth = (GaussianKernelRadius * 2) + 1;
        var entries = new GaussianKernelEntry[kernelWidth * kernelWidth];
        var index = 0;
        var total = 0d;

        for (var offsetY = -GaussianKernelRadius; offsetY <= GaussianKernelRadius; offsetY++)
        {
            for (var offsetX = -GaussianKernelRadius; offsetX <= GaussianKernelRadius; offsetX++)
            {
                var weight = Math.Exp(-((offsetX * offsetX) + (offsetY * offsetY)) / (2d * GaussianSigma * GaussianSigma));
                entries[index++] = new GaussianKernelEntry(offsetX, offsetY, weight);
                total += weight;
            }
        }

        for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
        {
            entries[entryIndex] = entries[entryIndex] with { Weight = entries[entryIndex].Weight / total };
        }

        return entries;
    }

    private static GridCellKey SnapToGrid(double lat, double lng, double gridSizeMeters)
    {
        var clampedLat = Math.Clamp(lat, -85d, 85d);
        var x = EarthRadiusMeters * DegreesToRadians(lng);
        var y = EarthRadiusMeters * Math.Log(Math.Tan((Math.PI / 4d) + (DegreesToRadians(clampedLat) / 2d)));

        return new GridCellKey(
            (int)Math.Round(x / gridSizeMeters, MidpointRounding.AwayFromZero),
            (int)Math.Round(y / gridSizeMeters, MidpointRounding.AwayFromZero));
    }

    private static HeatmapCoordinate GetGridCellCenter(GridCellKey cell, double gridSizeMeters)
    {
        var x = cell.X * gridSizeMeters;
        var y = cell.Y * gridSizeMeters;

        return new HeatmapCoordinate(
            RadiansToDegrees(2d * Math.Atan(Math.Exp(y / EarthRadiusMeters)) - (Math.PI / 2d)),
            RadiansToDegrees(x / EarthRadiusMeters));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;

    private static double RadiansToDegrees(double radians) => radians * 180d / Math.PI;

    private readonly record struct GridCellKey(int X, int Y);

    private readonly record struct HeatmapCoordinate(double Lat, double Lng);

    private readonly record struct GaussianKernelEntry(int OffsetX, int OffsetY, double Weight);

    private sealed class MovementVisitRecord
    {
        public string DeviceId { get; init; } = string.Empty;

        public int PoiId { get; init; }

        public string PoiName { get; init; } = string.Empty;

        public double PoiLat { get; init; }

        public double PoiLng { get; init; }

        public DateTime CreatedAt { get; init; }
    }

    private sealed class HeatmapVisitRecord
    {
        public string DeviceId { get; init; } = string.Empty;

        public double Lat { get; init; }

        public double Lng { get; init; }

        public DateTime CreatedAt { get; init; }
    }

    private sealed class MovementFlowAggregate
    {
        public int FromPoiId { get; init; }

        public string FromPoiName { get; init; } = string.Empty;

        public double FromLat { get; init; }

        public double FromLng { get; init; }

        public int ToPoiId { get; init; }

        public string ToPoiName { get; init; } = string.Empty;

        public double ToLat { get; init; }

        public double ToLng { get; init; }

        public int Weight { get; set; }

        public int UniqueSessions { get; set; }
    }
}
