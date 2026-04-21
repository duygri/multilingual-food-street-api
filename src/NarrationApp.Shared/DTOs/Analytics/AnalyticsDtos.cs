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

    public int Weight { get; init; }
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
