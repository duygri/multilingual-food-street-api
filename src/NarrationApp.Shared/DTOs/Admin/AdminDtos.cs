using NarrationApp.Shared.Enums;

namespace NarrationApp.Shared.DTOs.Admin;

public sealed class AdminPoiDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string OwnerName { get; init; } = string.Empty;

    public string OwnerEmail { get; init; } = string.Empty;

    public double Lat { get; init; }

    public double Lng { get; init; }

    public int Priority { get; init; }

    public int? CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public string Description { get; init; } = string.Empty;

    public string TtsScript { get; init; } = string.Empty;

    public PoiStatus Status { get; init; }

    public int AudioAssetCount { get; init; }

    public int TranslationCount { get; init; }

    public int GeofenceCount { get; init; }

    public int? PendingModerationId { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class UserSummaryDto
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string DeviceId { get; init; } = string.Empty;

    public string PreferredLanguage { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public string RoleName { get; init; } = string.Empty;

    public bool IsOnline { get; init; }

    public int DeviceCount { get; init; }

    public int ActiveDeviceCount { get; init; }

    public DateTime? LastSeenAtUtc { get; init; }
}

public sealed class VisitorDeviceSummaryDto
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string AccountLabel { get; init; } = string.Empty;

    public string DeviceId { get; init; } = string.Empty;

    public string PreferredLanguage { get; init; } = string.Empty;

    public string RoleName { get; init; } = string.Empty;

    public bool IsOnline { get; init; }

    public bool AutoPlayEnabled { get; init; }

    public bool BackgroundTrackingEnabled { get; init; }

    public int TrackingCount { get; init; }

    public int VisitCount { get; init; }

    public int TriggerCount { get; init; }

    public DateTime? LastSeenAtUtc { get; init; }
}

public sealed class UpdateUserRoleRequest
{
    public UserRole Role { get; init; }
}
