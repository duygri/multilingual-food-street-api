using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Controllers;

public sealed partial class OwnerController
{
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
