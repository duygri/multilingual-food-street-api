using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

internal static class OwnerWorkspaceResponseBuilder
{
    public static OwnerDashboardWorkspaceDto BuildDashboardWorkspace(
        IReadOnlyList<Poi> ownerPois,
        IReadOnlyList<AudioAsset> audioAssets,
        IReadOnlyList<VisitEvent> visitEvents,
        IReadOnlyList<ModerationRequest> moderationRequests)
    {
        var poiLookup = ownerPois.ToDictionary(item => item.Id);

        return new OwnerDashboardWorkspaceDto
        {
            Summary = new OwnerWorkspaceSummaryDto
            {
                TotalPois = ownerPois.Count,
                PublishedPois = ownerPois.Count(item => item.Status == PoiStatus.Published),
                PendingReviewPois = ownerPois.Count(item => item.Status == PoiStatus.PendingReview),
                ReadyAudioAssets = audioAssets.Count(item => item.Status == AudioStatus.Ready)
            },
            PublishedRows = ownerPois
                .Where(item => item.Status == PoiStatus.Published)
                .OrderByDescending(item => item.Priority)
                .ThenBy(item => item.Name)
                .Select(item => new OwnerDashboardPublishedRowDto
                {
                    PoiId = item.Id,
                    PoiName = item.Name,
                    ImageUrl = item.ImageUrl,
                    CategoryName = item.Category?.Name,
                    ListenCount = visitEvents.Count(eventItem => eventItem.PoiId == item.Id && eventItem.EventType == EventType.AudioPlay),
                    Trend = BuildSevenDayTrend(visitEvents.Where(eventItem => eventItem.PoiId == item.Id && eventItem.EventType == EventType.AudioPlay)),
                    LocationHint = $"{item.Lat:0.####}, {item.Lng:0.####}"
                })
                .ToArray(),
            RecentActivities = BuildDashboardRecentActivities(moderationRequests, audioAssets, poiLookup)
        };
    }

    public static OwnerPoisWorkspaceDto BuildPoisWorkspace(
        IReadOnlyList<Poi> ownerPois,
        IReadOnlyList<AudioAsset> audioAssets)
    {
        return new OwnerPoisWorkspaceDto
        {
            Summary = new OwnerPoisWorkspaceSummaryDto
            {
                TotalPois = ownerPois.Count,
                PublishedPois = ownerPois.Count(item => item.Status == PoiStatus.Published),
                PendingReviewPois = ownerPois.Count(item => item.Status == PoiStatus.PendingReview),
                DraftOrRejectedPois = ownerPois.Count(item => item.Status is PoiStatus.Draft or PoiStatus.Rejected)
            },
            Rows = ownerPois.Select(item => new OwnerPoisWorkspaceRowDto
            {
                PoiId = item.Id,
                PoiName = item.Name,
                Slug = item.Slug,
                CategoryName = item.Category?.Name,
                Latitude = item.Lat,
                Longitude = item.Lng,
                Priority = item.Priority,
                ImageUrl = item.ImageUrl,
                SourceContentKind = ResolveSourceContentKind(item, audioAssets),
                Status = item.Status,
                CanResubmit = item.Status == PoiStatus.Rejected
            }).ToArray()
        };
    }

    public static OwnerModerationWorkspaceDto BuildModerationWorkspace(
        IReadOnlyList<Poi> ownerPois,
        IReadOnlyList<ModerationRequest> moderationRequests)
    {
        var ownerPoiLookup = ownerPois.ToDictionary(item => item.Id);

        return new OwnerModerationWorkspaceDto
        {
            Summary = new OwnerModerationWorkspaceSummaryDto
            {
                PendingCount = moderationRequests.Count(item => item.Status == ModerationStatus.Pending),
                ApprovedCount = moderationRequests.Count(item => item.Status == ModerationStatus.Approved),
                RejectedCount = moderationRequests.Count(item => item.Status == ModerationStatus.Rejected)
            },
            FlowState = new OwnerModerationFlowStateDto
            {
                DraftCount = ownerPois.Count(item => item.Status == PoiStatus.Draft),
                PendingCount = ownerPois.Count(item => item.Status == PoiStatus.PendingReview),
                NeedsChangesCount = ownerPois.Count(item => item.Status == PoiStatus.Rejected),
                ApprovedCount = ownerPois.Count(item => item.Status == PoiStatus.Published)
            },
            PendingRows = moderationRequests
                .Where(item => item.Status == ModerationStatus.Pending)
                .Select(item => BuildPendingModerationRow(item, ownerPoiLookup))
                .Where(item => item is not null)
                .Cast<OwnerModerationPendingRowDto>()
                .ToArray(),
            HistoryRows = moderationRequests
                .Where(item => item.Status != ModerationStatus.Pending)
                .Select(item => BuildHistoryModerationRow(item, ownerPoiLookup))
                .Where(item => item is not null)
                .Cast<OwnerModerationHistoryRowDto>()
                .ToArray()
        };
    }

    private static IReadOnlyList<int> BuildSevenDayTrend(IEnumerable<VisitEvent> visitEvents)
    {
        var groupedCounts = visitEvents
            .GroupBy(item => item.CreatedAt.Date)
            .ToDictionary(group => group.Key, group => group.Count());
        var today = DateTime.UtcNow.Date;
        var trend = new int[7];

        for (var index = 0; index < trend.Length; index++)
        {
            var day = today.AddDays(index - 6);
            trend[index] = groupedCounts.TryGetValue(day, out var count) ? count : 0;
        }

        return trend;
    }

    private static IReadOnlyList<OwnerDashboardRecentActivityDto> BuildDashboardRecentActivities(
        IEnumerable<ModerationRequest> moderationRequests,
        IEnumerable<AudioAsset> audioAssets,
        IReadOnlyDictionary<int, Poi> poiLookup)
    {
        var items = new List<OwnerDashboardRecentActivityDto>();

        foreach (var request in moderationRequests)
        {
            if (!int.TryParse(request.EntityId, out var poiId) || !poiLookup.TryGetValue(poiId, out var poi))
            {
                continue;
            }

            var (title, description, tone) = request.Status switch
            {
                ModerationStatus.Pending => (
                    $"Gửi duyệt POI mới: \"{poi.Name}\"",
                    "Owner đã gửi nội dung để admin xét duyệt.",
                    "warn"),
                ModerationStatus.Approved => (
                    $"Admin đã duyệt: \"{poi.Name}\"",
                    request.ReviewNote ?? "POI đã được admin phê duyệt.",
                    "good"),
                ModerationStatus.Rejected => (
                    $"Admin từ chối: \"{poi.Name}\"",
                    request.ReviewNote ?? "Owner cần cập nhật và gửi lại nội dung.",
                    "warn"),
                _ => (
                    $"Cập nhật moderation: \"{poi.Name}\"",
                    request.ReviewNote ?? "Moderation request đã thay đổi trạng thái.",
                    "info")
            };

            items.Add(new OwnerDashboardRecentActivityDto
            {
                Type = "moderation",
                Title = title,
                Description = description,
                OccurredAtUtc = request.CreatedAt,
                Tone = tone,
                LinkedPoiId = poi.Id
            });
        }

        foreach (var asset in audioAssets.Where(item => item.Status == AudioStatus.Ready))
        {
            if (!poiLookup.TryGetValue(asset.PoiId, out var poi))
            {
                continue;
            }

            items.Add(new OwnerDashboardRecentActivityDto
            {
                Type = "audio",
                Title = $"Audio ready: \"{poi.Name}\"",
                Description = $"Audio {asset.LanguageCode} đã sẵn sàng cho POI này.",
                OccurredAtUtc = asset.GeneratedAt ?? DateTime.MinValue,
                Tone = "good",
                LinkedPoiId = poi.Id
            });
        }

        return items
            .OrderByDescending(item => item.OccurredAtUtc)
            .Take(6)
            .ToArray();
    }

    private static OwnerSourceContentKind ResolveSourceContentKind(Poi poi, IEnumerable<AudioAsset> audioAssets)
    {
        var hasVietnameseAudio = audioAssets.Any(item =>
            item.PoiId == poi.Id
            && string.Equals(item.LanguageCode, AppConstants.DefaultLanguage, StringComparison.OrdinalIgnoreCase)
            && item.Status == AudioStatus.Ready);

        if (hasVietnameseAudio)
        {
            return OwnerSourceContentKind.AudioFile;
        }

        return string.IsNullOrWhiteSpace(poi.TtsScript)
            ? OwnerSourceContentKind.None
            : OwnerSourceContentKind.ScriptTts;
    }

    private static OwnerModerationPendingRowDto? BuildPendingModerationRow(
        ModerationRequest request,
        IReadOnlyDictionary<int, Poi> poiLookup)
    {
        if (!int.TryParse(request.EntityId, out var poiId) || !poiLookup.TryGetValue(poiId, out var poi))
        {
            return null;
        }

        return new OwnerModerationPendingRowDto
        {
            ModerationRequestId = request.Id,
            PoiId = poi.Id,
            PoiName = poi.Name,
            RequestType = "Gửi duyệt",
            SubmittedAtUtc = request.CreatedAt,
            WaitTimeLabel = BuildRelativeTimeLabel(request.CreatedAt),
            ActionLabel = "Mở POI"
        };
    }

    private static OwnerModerationHistoryRowDto? BuildHistoryModerationRow(
        ModerationRequest request,
        IReadOnlyDictionary<int, Poi> poiLookup)
    {
        if (!int.TryParse(request.EntityId, out var poiId) || !poiLookup.TryGetValue(poiId, out var poi))
        {
            return null;
        }

        return new OwnerModerationHistoryRowDto
        {
            ModerationRequestId = request.Id,
            PoiId = poi.Id,
            PoiName = poi.Name,
            RequestType = "Gửi duyệt",
            SubmittedAtUtc = request.CreatedAt,
            ReviewedAtUtc = request.ReviewedBy is null ? null : request.CreatedAt,
            Result = request.Status switch
            {
                ModerationStatus.Approved => "Đã duyệt",
                ModerationStatus.Rejected => "Bị từ chối",
                ModerationStatus.Revised => "Đã chỉnh sửa",
                _ => request.Status.ToString()
            },
            AdminNote = request.ReviewNote,
            ActionLabel = request.Status == ModerationStatus.Rejected ? "Sửa trong POI detail" : "Xem POI"
        };
    }

    private static string BuildRelativeTimeLabel(DateTime createdAtUtc)
    {
        var elapsed = DateTime.UtcNow - createdAtUtc;

        if (elapsed.TotalMinutes < 60)
        {
            return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalMinutes))} phút";
        }

        if (elapsed.TotalHours < 24)
        {
            return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalHours))} giờ";
        }

        return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalDays))} ngày";
    }
}
