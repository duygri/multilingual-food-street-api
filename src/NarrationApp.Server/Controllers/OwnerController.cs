using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
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
    [HttpGet("pois")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PoiDto>>>> GetPoisAsync(CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var pois = await dbContext.Pois
            .AsNoTracking()
            .Where(item => item.OwnerId == ownerId)
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
}
