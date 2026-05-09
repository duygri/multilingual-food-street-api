using NarrationApp.Shared.DTOs.Poi;

namespace NarrationApp.Server.Services;

public interface IPoiReviewService
{
    Task<PoiReviewDto> CreateAsync(int poiId, CreatePoiReviewRequest request, CancellationToken cancellationToken = default);

    Task<PoiReviewSummaryDto> GetSummaryAsync(int poiId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PoiReviewDto>> GetPendingAsync(CancellationToken cancellationToken = default);

    Task<PoiReviewDto> ReviewAsync(int reviewId, bool approve, string? reviewNote, CancellationToken cancellationToken = default);
}
