using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Moderation;

namespace NarrationApp.Web.Services;

public interface IAdminPortalService
{
    Task<DashboardDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminPoiDto>> GetPoisAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VisitorDeviceSummaryDto>> GetVisitorDevicesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModerationRequestDto>> GetPendingModerationAsync(CancellationToken cancellationToken = default);

    Task<ModerationRequestDto> ApproveModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default);

    Task<ModerationRequestDto> RejectModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default);

    Task<AnalyticsSnapshotDto> GetAnalyticsSnapshotAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AnalyticsSnapshotDto());

    Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(HeatmapQueryDto query, CancellationToken cancellationToken = default) =>
        GetHeatmapAsync(cancellationToken);

    Task<IReadOnlyList<MovementFlowDto>> GetMovementFlowsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MovementFlowDto>>(Array.Empty<MovementFlowDto>());

    Task<IReadOnlyList<MovementFlowDto>> GetMovementFlowsAsync(MovementFlowQueryDto query, CancellationToken cancellationToken = default) =>
        GetMovementFlowsAsync(cancellationToken);

    Task<IReadOnlyList<TopPoiDto>> GetTopPoisAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PoiAverageListenDto>> GetAverageListenByPoiAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PoiAverageListenDto>>(Array.Empty<PoiAverageListenDto>());

    Task<AudioPlayAnalyticsDto> GetAudioPlayAnalyticsAsync(CancellationToken cancellationToken = default);

    Task UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);
}
