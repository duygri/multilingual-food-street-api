using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Moderation;

namespace NarrationApp.Web.Services;

public interface IAdminPortalService
{
    Task<DashboardDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminPoiDto>> GetPoisAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModerationRequestDto>> GetPendingModerationAsync(CancellationToken cancellationToken = default);

    Task<ModerationRequestDto> ApproveModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default);

    Task<ModerationRequestDto> RejectModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopPoiDto>> GetTopPoisAsync(CancellationToken cancellationToken = default);

    Task<AudioPlayAnalyticsDto> GetAudioPlayAnalyticsAsync(CancellationToken cancellationToken = default);

    Task UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);
}
