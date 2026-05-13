using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Extensions;
using NarrationApp.Server.Services;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Authorize(Roles = "poi_owner")]
[Route("api/owner")]
public sealed partial class OwnerController(AppDbContext dbContext, INotificationService notificationService) : ControllerBase
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
        var response = OwnerWorkspaceResponseBuilder.BuildDashboardWorkspace(ownerPois, audioAssets, visitEvents, moderationRequests);

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
        var response = OwnerWorkspaceResponseBuilder.BuildPoisWorkspace(ownerPois, audioAssets);

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
        var moderationRequests = await dbContext.ModerationRequests
            .AsNoTracking()
            .Where(item => item.RequestedBy == ownerId && item.EntityType == "poi")
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        var response = OwnerWorkspaceResponseBuilder.BuildModerationWorkspace(ownerPois, moderationRequests);

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
}
