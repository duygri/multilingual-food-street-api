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
        var ownerPoiIds = dbContext.Pois
            .Where(item => item.OwnerId == ownerId)
            .Select(item => item.Id);

        var response = new OwnerDashboardDto
        {
            TotalPois = await dbContext.Pois.CountAsync(item => item.OwnerId == ownerId, cancellationToken),
            PublishedPois = await dbContext.Pois.CountAsync(item => item.OwnerId == ownerId && item.Status == PoiStatus.Published, cancellationToken),
            DraftPois = await dbContext.Pois.CountAsync(item => item.OwnerId == ownerId && item.Status == PoiStatus.Draft, cancellationToken),
            PendingReviewPois = await dbContext.Pois.CountAsync(item => item.OwnerId == ownerId && item.Status == PoiStatus.PendingReview, cancellationToken),
            TotalAudioAssets = await dbContext.AudioAssets.CountAsync(item => ownerPoiIds.Contains(item.PoiId), cancellationToken),
            PendingModerationRequests = await dbContext.ModerationRequests.CountAsync(item => item.RequestedBy == ownerId && item.Status == ModerationStatus.Pending, cancellationToken),
            UnreadNotifications = (await notificationService.GetUnreadCountAsync(ownerId, cancellationToken)).Count
        };

        return Ok(new ApiResponse<OwnerDashboardDto> { Succeeded = true, Message = "Owner dashboard loaded.", Data = response });
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
