using NarrationApp.Shared.Enums;

namespace NarrationApp.Shared.DTOs.Owner;

public sealed class OwnerWorkspaceSummaryDto
{
    public int TotalPois { get; init; }

    public int PublishedPois { get; init; }

    public int PendingReviewPois { get; init; }

    public int ReadyAudioAssets { get; init; }
}

public sealed class OwnerDashboardWorkspaceDto
{
    public OwnerWorkspaceSummaryDto Summary { get; init; } = new();

    public IReadOnlyList<OwnerDashboardPublishedRowDto> PublishedRows { get; init; } = Array.Empty<OwnerDashboardPublishedRowDto>();

    public IReadOnlyList<OwnerDashboardRecentActivityDto> RecentActivities { get; init; } = Array.Empty<OwnerDashboardRecentActivityDto>();
}

public sealed class OwnerDashboardPublishedRowDto
{
    public int PoiId { get; init; }

    public string PoiName { get; init; } = string.Empty;

    public string? ImageUrl { get; init; }

    public string? CategoryName { get; init; }

    public int ListenCount { get; init; }

    public IReadOnlyList<int> Trend { get; init; } = Array.Empty<int>();

    public string LocationHint { get; init; } = string.Empty;
}

public sealed class OwnerDashboardRecentActivityDto
{
    public string Type { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public DateTime OccurredAtUtc { get; init; }

    public string Tone { get; init; } = string.Empty;

    public int? LinkedPoiId { get; init; }
}

public sealed class OwnerPoisWorkspaceDto
{
    public OwnerPoisWorkspaceSummaryDto Summary { get; init; } = new();

    public IReadOnlyList<OwnerPoisWorkspaceRowDto> Rows { get; init; } = Array.Empty<OwnerPoisWorkspaceRowDto>();
}

public sealed class OwnerPoisWorkspaceSummaryDto
{
    public int TotalPois { get; init; }

    public int PublishedPois { get; init; }

    public int PendingReviewPois { get; init; }

    public int DraftOrRejectedPois { get; init; }
}

public sealed class OwnerPoisWorkspaceRowDto
{
    public int PoiId { get; init; }

    public string PoiName { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? CategoryName { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public int Priority { get; init; }

    public string? ImageUrl { get; init; }

    public OwnerSourceContentKind SourceContentKind { get; init; }

    public PoiStatus Status { get; init; }

    public bool CanResubmit { get; init; }
}

public enum OwnerSourceContentKind
{
    None = 0,
    ScriptTts = 1,
    AudioFile = 2
}

public sealed class OwnerPoiDetailWorkspaceDto
{
    public OwnerPoiDetailSummaryDto Summary { get; init; } = new();

    public OwnerPoiDetailMetricsDto Metrics { get; init; } = new();
}

public sealed class OwnerPoiDetailSummaryDto
{
    public int PoiId { get; init; }

    public string PoiName { get; init; } = string.Empty;

    public string? ImageUrl { get; init; }

    public PoiStatus Status { get; init; }

    public string? CategoryName { get; init; }
}

public sealed class OwnerPoiDetailMetricsDto
{
    public int TotalVisits { get; init; }

    public int AudioPlays { get; init; }

    public int TranslationCount { get; init; }

    public int AudioAssetCount { get; init; }

    public int GeofenceCount { get; init; }

    public int QrScans { get; init; }

    public double TotalListenDurationSeconds { get; init; }
}

public sealed class OwnerModerationWorkspaceDto
{
    public OwnerModerationWorkspaceSummaryDto Summary { get; init; } = new();

    public OwnerModerationFlowStateDto FlowState { get; init; } = new();

    public IReadOnlyList<OwnerModerationPendingRowDto> PendingRows { get; init; } = Array.Empty<OwnerModerationPendingRowDto>();

    public IReadOnlyList<OwnerModerationHistoryRowDto> HistoryRows { get; init; } = Array.Empty<OwnerModerationHistoryRowDto>();
}

public sealed class OwnerModerationWorkspaceSummaryDto
{
    public int PendingCount { get; init; }

    public int ApprovedCount { get; init; }

    public int RejectedCount { get; init; }
}

public sealed class OwnerModerationFlowStateDto
{
    public int DraftCount { get; init; }

    public int PendingCount { get; init; }

    public int NeedsChangesCount { get; init; }

    public int ApprovedCount { get; init; }
}

public sealed class OwnerModerationPendingRowDto
{
    public int ModerationRequestId { get; init; }

    public int PoiId { get; init; }

    public string PoiName { get; init; } = string.Empty;

    public string RequestType { get; init; } = string.Empty;

    public DateTime SubmittedAtUtc { get; init; }

    public string WaitTimeLabel { get; init; } = string.Empty;

    public string ActionLabel { get; init; } = string.Empty;
}

public sealed class OwnerModerationHistoryRowDto
{
    public int ModerationRequestId { get; init; }

    public int PoiId { get; init; }

    public string PoiName { get; init; } = string.Empty;

    public string RequestType { get; init; } = string.Empty;

    public DateTime SubmittedAtUtc { get; init; }

    public DateTime? ReviewedAtUtc { get; init; }

    public string Result { get; init; } = string.Empty;

    public string? AdminNote { get; init; }

    public string ActionLabel { get; init; } = string.Empty;
}
