using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class PoiReviewService(AppDbContext dbContext) : IPoiReviewService
{
    private const int MaxCommentLength = 600;
    private const int MaxReviewNoteLength = 600;
    private const int LatestApprovedReviewLimit = 5;

    public async Task<PoiReviewDto> CreateAsync(int poiId, CreatePoiReviewRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);

        var poiExists = await dbContext.Pois
            .AsNoTracking()
            .AnyAsync(item => item.Id == poiId && item.Status == PoiStatus.Published, cancellationToken);

        if (!poiExists)
        {
            throw new KeyNotFoundException("POI was not found.");
        }

        var review = new PoiReview
        {
            PoiId = poiId,
            DeviceId = request.DeviceId.Trim(),
            Rating = request.Rating,
            Comment = NormalizeOptional(request.Comment),
            Status = PoiReviewStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.PoiReviews.Add(review);
        await dbContext.SaveChangesAsync(cancellationToken);

        return review.ToDto();
    }

    public async Task<PoiReviewSummaryDto> GetSummaryAsync(int poiId, CancellationToken cancellationToken = default)
    {
        var approvedReviews = await dbContext.PoiReviews
            .AsNoTracking()
            .Where(review => review.PoiId == poiId && review.Status == PoiReviewStatus.Approved)
            .OrderByDescending(review => review.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return new PoiReviewSummaryDto
        {
            PoiId = poiId,
            ApprovedCount = approvedReviews.Count,
            AverageRating = approvedReviews.Count == 0
                ? 0
                : Math.Round(approvedReviews.Average(review => review.Rating), 2, MidpointRounding.AwayFromZero),
            LatestApprovedReviews = approvedReviews
                .Take(LatestApprovedReviewLimit)
                .Select(review => review.ToDto())
                .ToArray()
        };
    }

    public async Task<IReadOnlyList<PoiReviewDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var pendingReviews = await dbContext.PoiReviews
            .AsNoTracking()
            .Where(review => review.Status == PoiReviewStatus.Pending)
            .OrderBy(review => review.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return pendingReviews.Select(review => review.ToDto()).ToArray();
    }

    public async Task<PoiReviewDto> ReviewAsync(int reviewId, bool approve, string? reviewNote, CancellationToken cancellationToken = default)
    {
        var review = await dbContext.PoiReviews
            .SingleOrDefaultAsync(item => item.Id == reviewId, cancellationToken)
            ?? throw new KeyNotFoundException("POI review was not found.");

        var normalizedReviewNote = NormalizeOptional(reviewNote);
        if (normalizedReviewNote?.Length > MaxReviewNoteLength)
        {
            throw new InvalidOperationException($"Review note must be at most {MaxReviewNoteLength} characters.");
        }

        review.Status = approve ? PoiReviewStatus.Approved : PoiReviewStatus.Rejected;
        review.ReviewNote = normalizedReviewNote;
        review.ReviewedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return review.ToDto();
    }

    private static void ValidateCreateRequest(CreatePoiReviewRequest request)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new InvalidOperationException("Rating must be between 1 and 5.");
        }

        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            throw new InvalidOperationException("Device id is required.");
        }

        if (request.Comment?.Trim().Length > MaxCommentLength)
        {
            throw new InvalidOperationException($"Comment must be at most {MaxCommentLength} characters.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

internal static class PoiReviewMappingExtensions
{
    public static PoiReviewDto ToDto(this PoiReview review)
    {
        return new PoiReviewDto
        {
            Id = review.Id,
            PoiId = review.PoiId,
            DeviceId = review.DeviceId,
            Rating = review.Rating,
            Comment = review.Comment,
            Status = review.Status,
            ReviewNote = review.ReviewNote,
            CreatedAtUtc = review.CreatedAtUtc,
            ReviewedAtUtc = review.ReviewedAtUtc
        };
    }
}
