using NarrationApp.Shared.Enums;

namespace NarrationApp.Shared.DTOs.Analytics;

public sealed class DashboardDto
{
    public int TotalPois { get; init; }

    public int PublishedPois { get; init; }

    public int TotalTours { get; init; }

    public int TotalAudioAssets { get; init; }

    public int PendingModerationRequests { get; init; }

    public int UnreadNotifications { get; init; }

    public IReadOnlyList<TopPoiDto> TopPois { get; init; } = Array.Empty<TopPoiDto>();
}

public sealed class HeatmapPointDto
{
    public double Lat { get; init; }

    public double Lng { get; init; }

    public double Weight { get; init; }
}

public enum HeatmapTimeRange
{
    Last24Hours = 1,
    Last7Days = 2,
    Last30Days = 3,
    AllTime = 4
}

public sealed class HeatmapQueryDto
{
    public HeatmapTimeRange TimeRange { get; init; } = HeatmapTimeRange.Last7Days;

    public EventType? EventTypeFilter { get; init; }

    public bool UseTimeDecay { get; init; } = true;

    public double GridSizeMeters { get; init; } = 50d;

    public double MaxWeight { get; init; } = 50d;

    public bool ApplyGaussianSmoothing { get; init; } = true;

    public DateTime? ReferenceTimeUtc { get; init; }
}

public sealed class MovementFlowQueryDto
{
    public HeatmapTimeRange TimeRange { get; init; } = HeatmapTimeRange.Last7Days;

    public EventType? EventTypeFilter { get; init; }

    public int MinimumUniqueSessions { get; init; } = 3;

    public DateTime? ReferenceTimeUtc { get; init; }
}

public sealed class AnalyticsSnapshotDto
{
    public int GeofenceTriggers { get; init; }

    public int AudioPlays { get; init; }

    public int QrScans { get; init; }

    public double AverageListenDurationSeconds { get; init; }
}

public sealed class MovementFlowDto
{
    public int FromPoiId { get; init; }

    public string FromPoiName { get; init; } = string.Empty;

    public double FromLat { get; init; }

    public double FromLng { get; init; }

    public int ToPoiId { get; init; }

    public string ToPoiName { get; init; } = string.Empty;

    public double ToLat { get; init; }

    public double ToLng { get; init; }

    public int Weight { get; init; }

    public int UniqueSessions { get; init; }
}

public sealed class PoiAverageListenDto
{
    public int PoiId { get; init; }

    public string PoiName { get; init; } = string.Empty;

    public double AverageListenDurationSeconds { get; init; }

    public int AudioPlayCount { get; init; }
}

public sealed class TopPoiDto
{
    public int PoiId { get; init; }

    public string PoiName { get; init; } = string.Empty;

    public int Visits { get; init; }
}

public sealed class PoiAnalyticsDto
{
    public int PoiId { get; init; }

    public int TotalVisits { get; init; }

    public int AudioPlays { get; init; }
}

public sealed class AudioPlayAnalyticsDto
{
    public int TotalAudioPlays { get; init; }

    public int TotalListenSeconds { get; init; }
}
