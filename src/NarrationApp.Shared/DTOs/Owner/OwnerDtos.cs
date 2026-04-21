namespace NarrationApp.Shared.DTOs.Owner;

public sealed class OwnerDashboardDto
{
    public int TotalPois { get; init; }

    public int PublishedPois { get; init; }

    public int DraftPois { get; init; }

    public int PendingReviewPois { get; init; }

    public int TotalAudioAssets { get; init; }

    public int PendingModerationRequests { get; init; }

    public int UnreadNotifications { get; init; }
}

public sealed class OwnerPoiStatsDto
{
    public int PoiId { get; init; }

    public int TotalVisits { get; init; }

    public int AudioPlays { get; init; }

    public int TranslationCount { get; init; }

    public int AudioAssetCount { get; init; }

    public int GeofenceCount { get; init; }
}
