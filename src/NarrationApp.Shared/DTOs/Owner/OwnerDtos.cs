namespace NarrationApp.Shared.DTOs.Owner;

public sealed class OwnerShellSummaryDto
{
    public int TotalPois { get; init; }

    public int PublishedPois { get; init; }

    public int PendingModerationRequests { get; init; }

    public int UnreadNotifications { get; init; }
}

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

public sealed class OwnerProfileDto
{
    public Guid UserId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? ManagedArea { get; init; }

    public string PreferredLanguage { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? LastLoginAtUtc { get; init; }

    public OwnerActivitySummaryDto ActivitySummary { get; init; } = new();
}

public sealed class OwnerActivitySummaryDto
{
    public int TotalPois { get; init; }

    public int PublishedPois { get; init; }

    public int DraftPois { get; init; }

    public int PendingReviewPois { get; init; }

    public int TotalAudioAssets { get; init; }

    public int TotalAudioPlays { get; init; }

    public int UnreadNotifications { get; init; }
}

public sealed class UpdateOwnerProfileRequest
{
    public string FullName { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? ManagedArea { get; init; }

    public string PreferredLanguage { get; init; } = string.Empty;
}
