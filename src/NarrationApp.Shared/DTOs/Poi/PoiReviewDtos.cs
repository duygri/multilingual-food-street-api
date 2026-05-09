using NarrationApp.Shared.Enums;

namespace NarrationApp.Shared.DTOs.Poi;

public sealed class CreatePoiReviewRequest
{
    public string DeviceId { get; init; } = string.Empty;

    public int Rating { get; init; }

    public string? Comment { get; init; }
}

public sealed class PoiReviewDto
{
    public int Id { get; init; }

    public int PoiId { get; init; }

    public string DeviceId { get; init; } = string.Empty;

    public int Rating { get; init; }

    public string? Comment { get; init; }

    public PoiReviewStatus Status { get; init; }

    public string? ReviewNote { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? ReviewedAtUtc { get; init; }
}

public sealed class PoiReviewSummaryDto
{
    public int PoiId { get; init; }

    public int ApprovedCount { get; init; }

    public double AverageRating { get; init; }

    public IReadOnlyList<PoiReviewDto> LatestApprovedReviews { get; init; } = Array.Empty<PoiReviewDto>();
}

public sealed class ReviewPoiReviewRequest
{
    public string? ReviewNote { get; init; }
}
