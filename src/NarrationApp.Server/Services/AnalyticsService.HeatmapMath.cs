using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed partial class AnalyticsService
{
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

    private static bool IsWithinDistrict4Bounds(double lat, double lng)
    {
        return lat >= District4MinimumLatitude
            && lat <= District4MaximumLatitude
            && lng >= District4MinimumLongitude
            && lng <= District4MaximumLongitude;
    }

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
