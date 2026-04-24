using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Extensions;
using NarrationApp.Server.Services;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Authorize(Roles = "poi_owner")]
[Route("api/owner")]
public sealed class OwnerController(AppDbContext dbContext, INotificationService notificationService) : ControllerBase
{
    private const int FullNameMaxLength = 150;
    private const int PhoneMaxLength = 30;
    private const int ManagedAreaMaxLength = 250;
    private const int PreferredLanguageMaxLength = 10;

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<OwnerProfileDto>>> GetProfileAsync(CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var owner = await dbContext.AppUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == ownerId, cancellationToken);

        if (owner is null)
        {
            return NotFound(new ApiResponse<OwnerProfileDto>
            {
                Succeeded = false,
                Message = "Owner profile not found.",
                Error = new ErrorResponse { Code = "owner_not_found", Message = "Owner profile was not found." }
            });
        }

        return Ok(new ApiResponse<OwnerProfileDto>
        {
            Succeeded = true,
            Message = "Owner profile loaded.",
            Data = await BuildProfileAsync(owner, cancellationToken)
        });
    }

    [HttpGet("pois")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PoiDto>>>> GetPoisAsync(CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var pois = await dbContext.Pois
            .AsNoTracking()
            .Where(item => item.OwnerId == ownerId)
            .Include(item => item.Category)
            .Include(item => item.Translations)
            .Include(item => item.Geofences)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<IReadOnlyList<PoiDto>>
        {
            Succeeded = true,
            Message = "Owner POIs loaded.",
            Data = pois.Select(item => item.ToDto()).ToArray()
        });
    }

    [HttpGet("pois/{id:int}")]
    public async Task<ActionResult<ApiResponse<PoiDto>>> GetPoiAsync(int id, CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var poi = await dbContext.Pois
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Translations)
            .Include(item => item.Geofences)
            .SingleOrDefaultAsync(item => item.Id == id && item.OwnerId == ownerId, cancellationToken);

        if (poi is null)
        {
            return NotFound(new ApiResponse<PoiDto>
            {
                Succeeded = false,
                Message = "POI not found.",
                Error = new ErrorResponse { Code = "poi_not_found", Message = "POI not found for this owner." }
            });
        }

        return Ok(new ApiResponse<PoiDto>
        {
            Succeeded = true,
            Message = "Owner POI loaded.",
            Data = poi.ToDto()
        });
    }

    [HttpGet("pois/{id:int}/stats")]
    public async Task<ActionResult<ApiResponse<OwnerPoiStatsDto>>> GetPoiStatsAsync(int id, CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var poi = await dbContext.Pois
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.OwnerId == ownerId, cancellationToken);

        if (poi is null)
        {
            return NotFound(new ApiResponse<OwnerPoiStatsDto>
            {
                Succeeded = false,
                Message = "POI not found.",
                Error = new ErrorResponse { Code = "poi_not_found", Message = "POI not found for this owner." }
            });
        }

        var response = new OwnerPoiStatsDto
        {
            PoiId = id,
            TotalVisits = await dbContext.VisitEvents.CountAsync(item => item.PoiId == id, cancellationToken),
            AudioPlays = await dbContext.VisitEvents.CountAsync(item => item.PoiId == id && item.EventType == EventType.AudioPlay, cancellationToken),
            TranslationCount = await dbContext.PoiTranslations.CountAsync(item => item.PoiId == id, cancellationToken),
            AudioAssetCount = await dbContext.AudioAssets.CountAsync(item => item.PoiId == id, cancellationToken),
            GeofenceCount = await dbContext.Geofences.CountAsync(item => item.PoiId == id, cancellationToken)
        };

        return Ok(new ApiResponse<OwnerPoiStatsDto> { Succeeded = true, Message = "Owner POI stats loaded.", Data = response });
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<OwnerDashboardDto>>> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var shellSummary = await BuildShellSummaryAsync(ownerId, cancellationToken);
        var ownerPoiIds = dbContext.Pois
            .Where(item => item.OwnerId == ownerId)
            .Select(item => item.Id);

        var response = new OwnerDashboardDto
        {
            TotalPois = shellSummary.TotalPois,
            PublishedPois = shellSummary.PublishedPois,
            DraftPois = await dbContext.Pois.CountAsync(item => item.OwnerId == ownerId && item.Status == PoiStatus.Draft, cancellationToken),
            PendingReviewPois = await dbContext.Pois.CountAsync(item => item.OwnerId == ownerId && item.Status == PoiStatus.PendingReview, cancellationToken),
            TotalAudioAssets = await dbContext.AudioAssets.CountAsync(item => ownerPoiIds.Contains(item.PoiId), cancellationToken),
            PendingModerationRequests = shellSummary.PendingModerationRequests,
            UnreadNotifications = shellSummary.UnreadNotifications
        };

        return Ok(new ApiResponse<OwnerDashboardDto> { Succeeded = true, Message = "Owner dashboard loaded.", Data = response });
    }

    [HttpGet("dashboard/workspace")]
    public async Task<ActionResult<ApiResponse<OwnerDashboardWorkspaceDto>>> GetDashboardWorkspaceAsync(CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var ownerPois = await dbContext.Pois
            .AsNoTracking()
            .Where(item => item.OwnerId == ownerId)
            .Include(item => item.Category)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        var ownerPoiIds = ownerPois.Select(item => item.Id).ToArray();
        var audioAssets = await dbContext.AudioAssets
            .AsNoTracking()
            .Where(item => ownerPoiIds.Contains(item.PoiId))
            .ToListAsync(cancellationToken);
        var visitEvents = await dbContext.VisitEvents
            .AsNoTracking()
            .Where(item => ownerPoiIds.Contains(item.PoiId))
            .ToListAsync(cancellationToken);
        var moderationRequests = await dbContext.ModerationRequests
            .AsNoTracking()
            .Where(item => item.RequestedBy == ownerId && item.EntityType == "poi")
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        var poiLookup = ownerPois.ToDictionary(item => item.Id);

        var response = new OwnerDashboardWorkspaceDto
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

        return Ok(new ApiResponse<OwnerDashboardWorkspaceDto>
        {
            Succeeded = true,
            Message = "Owner dashboard workspace loaded.",
            Data = response
        });
    }

    [HttpGet("pois/workspace")]
    public async Task<ActionResult<ApiResponse<OwnerPoisWorkspaceDto>>> GetPoisWorkspaceAsync(CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var ownerPois = await dbContext.Pois
            .AsNoTracking()
            .Where(item => item.OwnerId == ownerId)
            .Include(item => item.Category)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        var ownerPoiIds = ownerPois.Select(item => item.Id).ToArray();
        var audioAssets = await dbContext.AudioAssets
            .AsNoTracking()
            .Where(item => ownerPoiIds.Contains(item.PoiId))
            .ToListAsync(cancellationToken);

        var response = new OwnerPoisWorkspaceDto
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

        return Ok(new ApiResponse<OwnerPoisWorkspaceDto>
        {
            Succeeded = true,
            Message = "Owner POI workspace loaded.",
            Data = response
        });
    }

    [HttpGet("pois/{id:int}/workspace")]
    public async Task<ActionResult<ApiResponse<OwnerPoiDetailWorkspaceDto>>> GetPoiWorkspaceAsync(int id, CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var poi = await dbContext.Pois
            .AsNoTracking()
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == id && item.OwnerId == ownerId, cancellationToken);

        if (poi is null)
        {
            return NotFound(new ApiResponse<OwnerPoiDetailWorkspaceDto>
            {
                Succeeded = false,
                Message = "POI not found.",
                Error = new ErrorResponse { Code = "poi_not_found", Message = "POI not found for this owner." }
            });
        }

        var totalVisits = await dbContext.VisitEvents.CountAsync(item => item.PoiId == id, cancellationToken);
        var audioPlays = await dbContext.VisitEvents.CountAsync(item => item.PoiId == id && item.EventType == EventType.AudioPlay, cancellationToken);
        var translationCount = await dbContext.PoiTranslations.CountAsync(item => item.PoiId == id, cancellationToken);
        var audioAssetCount = await dbContext.AudioAssets.CountAsync(item => item.PoiId == id, cancellationToken);
        var geofenceCount = await dbContext.Geofences.CountAsync(item => item.PoiId == id, cancellationToken);
        var qrScans = await dbContext.VisitEvents.CountAsync(item => item.PoiId == id && item.EventType == EventType.QrScan, cancellationToken);
        var totalListenDurationSeconds = await dbContext.VisitEvents
            .Where(item => item.PoiId == id && item.EventType == EventType.AudioPlay)
            .SumAsync(item => (double?)item.ListenDurationSeconds, cancellationToken) ?? 0d;

        var response = new OwnerPoiDetailWorkspaceDto
        {
            Summary = new OwnerPoiDetailSummaryDto
            {
                PoiId = poi.Id,
                PoiName = poi.Name,
                ImageUrl = poi.ImageUrl,
                Status = poi.Status,
                CategoryName = poi.Category?.Name
            },
            Metrics = new OwnerPoiDetailMetricsDto
            {
                TotalVisits = totalVisits,
                AudioPlays = audioPlays,
                TranslationCount = translationCount,
                AudioAssetCount = audioAssetCount,
                GeofenceCount = geofenceCount,
                QrScans = qrScans,
                TotalListenDurationSeconds = totalListenDurationSeconds
            }
        };

        return Ok(new ApiResponse<OwnerPoiDetailWorkspaceDto>
        {
            Succeeded = true,
            Message = "Owner POI workspace loaded.",
            Data = response
        });
    }

    [HttpGet("moderation/workspace")]
    public async Task<ActionResult<ApiResponse<OwnerModerationWorkspaceDto>>> GetModerationWorkspaceAsync(CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var ownerPois = await dbContext.Pois
            .AsNoTracking()
            .Where(item => item.OwnerId == ownerId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        var ownerPoiLookup = ownerPois.ToDictionary(item => item.Id);
        var moderationRequests = await dbContext.ModerationRequests
            .AsNoTracking()
            .Where(item => item.RequestedBy == ownerId && item.EntityType == "poi")
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        var response = new OwnerModerationWorkspaceDto
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

        return Ok(new ApiResponse<OwnerModerationWorkspaceDto>
        {
            Succeeded = true,
            Message = "Owner moderation workspace loaded.",
            Data = response
        });
    }

    [HttpGet("shell-summary")]
    public async Task<ActionResult<ApiResponse<OwnerShellSummaryDto>>> GetShellSummaryAsync(CancellationToken cancellationToken)
    {
        var response = await BuildShellSummaryAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(new ApiResponse<OwnerShellSummaryDto> { Succeeded = true, Message = "Owner shell summary loaded.", Data = response });
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>>> GetNotificationsAsync(CancellationToken cancellationToken)
    {
        var response = await notificationService.GetByUserAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<NotificationDto>> { Succeeded = true, Message = "Owner notifications loaded.", Data = response });
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<OwnerProfileDto>>> UpdateProfileAsync(UpdateOwnerProfileRequest request, CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var owner = await dbContext.AppUsers
            .SingleOrDefaultAsync(item => item.Id == ownerId, cancellationToken);

        if (owner is null)
        {
            return NotFound(new ApiResponse<OwnerProfileDto>
            {
                Succeeded = false,
                Message = "Owner profile not found.",
                Error = new ErrorResponse { Code = "owner_not_found", Message = "Owner profile was not found." }
            });
        }

        var fullName = request.FullName?.Trim();
        var preferredLanguage = request.PreferredLanguage?.Trim().ToLowerInvariant();
        var phone = NormalizeOptionalField(request.Phone);
        var managedArea = NormalizeOptionalField(request.ManagedArea);
        var validationErrors = ValidateProfileUpdate(fullName, phone, managedArea, preferredLanguage);

        if (validationErrors.Count > 0)
        {
            return BadRequest(new ApiResponse<OwnerProfileDto>
            {
                Succeeded = false,
                Message = "Owner profile is invalid.",
                Error = new ErrorResponse
                {
                    Code = "invalid_owner_profile",
                    Message = "Owner profile contains invalid values.",
                    Details = validationErrors
                }
            });
        }

        owner.FullName = fullName!;
        owner.Phone = phone.WasProvided ? phone.Value : owner.Phone;
        owner.ManagedArea = managedArea.WasProvided ? managedArea.Value : owner.ManagedArea;
        owner.PreferredLanguage = preferredLanguage!;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<OwnerProfileDto>
        {
            Succeeded = true,
            Message = "Owner profile updated.",
            Data = await BuildProfileAsync(owner, cancellationToken)
        });
    }

    private async Task<OwnerProfileDto> BuildProfileAsync(AppUser owner, CancellationToken cancellationToken)
    {
        var ownerPoiIds = dbContext.Pois
            .AsNoTracking()
            .Where(item => item.OwnerId == owner.Id)
            .Select(item => item.Id);

        return new OwnerProfileDto
        {
            UserId = owner.Id,
            FullName = owner.FullName,
            Email = owner.Email,
            Phone = owner.Phone,
            ManagedArea = owner.ManagedArea,
            PreferredLanguage = owner.PreferredLanguage,
            CreatedAtUtc = owner.CreatedAtUtc,
            LastLoginAtUtc = owner.LastLoginAtUtc,
            ActivitySummary = new OwnerActivitySummaryDto
            {
                TotalPois = await dbContext.Pois.CountAsync(item => item.OwnerId == owner.Id, cancellationToken),
                PublishedPois = await dbContext.Pois.CountAsync(item => item.OwnerId == owner.Id && item.Status == PoiStatus.Published, cancellationToken),
                DraftPois = await dbContext.Pois.CountAsync(item => item.OwnerId == owner.Id && item.Status == PoiStatus.Draft, cancellationToken),
                PendingReviewPois = await dbContext.Pois.CountAsync(item => item.OwnerId == owner.Id && item.Status == PoiStatus.PendingReview, cancellationToken),
                TotalAudioAssets = await dbContext.AudioAssets.CountAsync(item => ownerPoiIds.Contains(item.PoiId), cancellationToken),
                TotalAudioPlays = await dbContext.VisitEvents.CountAsync(item => ownerPoiIds.Contains(item.PoiId) && item.EventType == EventType.AudioPlay, cancellationToken),
                UnreadNotifications = (await notificationService.GetUnreadCountAsync(owner.Id, cancellationToken)).Count
            }
        };
    }

    private async Task<OwnerShellSummaryDto> BuildShellSummaryAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return new OwnerShellSummaryDto
        {
            TotalPois = await dbContext.Pois.CountAsync(item => item.OwnerId == ownerId, cancellationToken),
            PublishedPois = await dbContext.Pois.CountAsync(item => item.OwnerId == ownerId && item.Status == PoiStatus.Published, cancellationToken),
            PendingModerationRequests = await dbContext.ModerationRequests.CountAsync(item => item.RequestedBy == ownerId && item.Status == ModerationStatus.Pending, cancellationToken),
            UnreadNotifications = (await notificationService.GetUnreadCountAsync(ownerId, cancellationToken)).Count
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

    private static OptionalProfileField NormalizeOptionalField(string? value)
    {
        if (value is null)
        {
            return new OptionalProfileField(false, null);
        }

        var normalizedValue = value.Trim();
        return new OptionalProfileField(true, normalizedValue.Length == 0 ? null : normalizedValue);
    }

    private static List<string> ValidateProfileUpdate(
        string? fullName,
        OptionalProfileField phone,
        OptionalProfileField managedArea,
        string? preferredLanguage)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors.Add("Full name is required.");
        }
        else if (fullName.Length > FullNameMaxLength)
        {
            errors.Add($"Full name must be {FullNameMaxLength} characters or fewer.");
        }

        if (phone.Value?.Length > PhoneMaxLength)
        {
            errors.Add($"Phone must be {PhoneMaxLength} characters or fewer.");
        }

        if (managedArea.Value?.Length > ManagedAreaMaxLength)
        {
            errors.Add($"Managed area must be {ManagedAreaMaxLength} characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(preferredLanguage))
        {
            errors.Add("Preferred language is required.");
        }
        else if (preferredLanguage.Length > PreferredLanguageMaxLength)
        {
            errors.Add($"Preferred language must be {PreferredLanguageMaxLength} characters or fewer.");
        }

        return errors;
    }

    private readonly record struct OptionalProfileField(bool WasProvided, string? Value);
}
