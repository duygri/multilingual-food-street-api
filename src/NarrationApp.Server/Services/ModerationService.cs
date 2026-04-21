using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class ModerationService(AppDbContext dbContext, INotificationService notificationService) : IModerationService
{
    public async Task<ModerationRequestDto> CreateAsync(Guid requestedBy, CreateModerationRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEntityType = request.EntityType.Trim().ToLowerInvariant();

        if (string.Equals(normalizedEntityType, "poi", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(request.EntityId, out var poiId))
        {
            var poi = await dbContext.Pois.SingleOrDefaultAsync(item => item.Id == poiId, cancellationToken)
                ?? throw new KeyNotFoundException("POI was not found.");

            if (poi.OwnerId != requestedBy)
            {
                throw new UnauthorizedAccessException("You cannot submit moderation for another owner's POI.");
            }

            poi.Status = PoiStatus.PendingReview;
        }

        var moderationRequest = new ModerationRequest
        {
            EntityType = normalizedEntityType,
            EntityId = request.EntityId,
            Status = ModerationStatus.Pending,
            RequestedBy = requestedBy,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.ModerationRequests.Add(moderationRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(moderationRequest);
    }

    public async Task<IReadOnlyList<ModerationRequestDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var items = await dbContext.ModerationRequests
            .AsNoTracking()
            .Where(item => item.Status == ModerationStatus.Pending)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(ToDto).ToArray();
    }

    public async Task<IReadOnlyList<ModerationRequestDto>> GetByRequesterAsync(Guid requestedBy, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.ModerationRequests
            .AsNoTracking()
            .Where(item => item.RequestedBy == requestedBy)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(ToDto).ToArray();
    }

    public async Task<ModerationRequestDto> ReviewAsync(int requestId, Guid reviewedBy, bool approved, string? reviewNote, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.ModerationRequests
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException("Moderation request was not found.");

        request.ReviewedBy = reviewedBy;
        request.ReviewNote = reviewNote;
        request.Status = approved ? ModerationStatus.Approved : ModerationStatus.Rejected;

        if (string.Equals(request.EntityType, "owner_registration", StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParse(request.EntityId, out var userId))
        {
            var owner = await dbContext.AppUsers.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken)
                ?? throw new KeyNotFoundException("Owner registration user was not found.");

            owner.IsActive = approved;
        }
        else if (string.Equals(request.EntityType, "poi", StringComparison.OrdinalIgnoreCase) &&
                 int.TryParse(request.EntityId, out var poiId))
        {
            var poi = await dbContext.Pois.SingleOrDefaultAsync(item => item.Id == poiId, cancellationToken)
                ?? throw new KeyNotFoundException("POI moderation target was not found.");

            poi.Status = approved ? PoiStatus.Published : PoiStatus.Rejected;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.CreateAsync(
            request.RequestedBy,
            NotificationType.ModerationResult,
            approved ? "Moderation approved" : "Moderation rejected",
            reviewNote ?? (approved ? "Your content was approved." : "Your content was rejected."),
            cancellationToken);

        return ToDto(request);
    }

    private static ModerationRequestDto ToDto(ModerationRequest request)
    {
        return new ModerationRequestDto
        {
            Id = request.Id,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            Status = request.Status,
            RequestedBy = request.RequestedBy,
            ReviewedBy = request.ReviewedBy,
            ReviewNote = request.ReviewNote,
            CreatedAtUtc = request.CreatedAt
        };
    }
}
