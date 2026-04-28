using System.Globalization;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Moderation;

namespace NarrationApp.Web.Services;

public sealed class AdminPortalService(ApiClient apiClient) : IAdminPortalService
{
    public Task<DashboardDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<DashboardDto>("api/admin/stats/overview", cancellationToken);
    }

    public Task<IReadOnlyList<AdminPoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<AdminPoiDto>>("api/admin/pois", cancellationToken);
    }

    public Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<UserSummaryDto>>("api/admin/users", cancellationToken);
    }

    public Task<IReadOnlyList<VisitorDeviceSummaryDto>> GetVisitorDevicesAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<VisitorDeviceSummaryDto>>("api/admin/visitor-devices", cancellationToken);
    }

    public Task<IReadOnlyList<ModerationRequestDto>> GetPendingModerationAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<ModerationRequestDto>>("api/admin/moderation/pending", cancellationToken);
    }

    public Task<ModerationRequestDto> ApproveModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<ReviewModerationRequest, ModerationRequestDto>($"api/admin/moderation/{requestId}/approve", request, cancellationToken);
    }

    public Task<ModerationRequestDto> RejectModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<ReviewModerationRequest, ModerationRequestDto>($"api/admin/moderation/{requestId}/reject", request, cancellationToken);
    }

    public Task<AnalyticsSnapshotDto> GetAnalyticsSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<AnalyticsSnapshotDto>("api/analytics/snapshot", cancellationToken);
    }

    public Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<HeatmapPointDto>>("api/analytics/heatmap", cancellationToken);
    }

    public Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(HeatmapQueryDto query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var gridSize = query.GridSizeMeters.ToString("0.##", CultureInfo.InvariantCulture);
        var maxWeight = query.MaxWeight.ToString("0.##", CultureInfo.InvariantCulture);
        var eventTypeSegment = query.EventTypeFilter.HasValue
            ? $"&eventTypeFilter={query.EventTypeFilter.Value}"
            : string.Empty;
        var uri = $"api/analytics/heatmap?timeRange={query.TimeRange}{eventTypeSegment}&useTimeDecay={query.UseTimeDecay.ToString().ToLowerInvariant()}&gridSizeMeters={gridSize}&maxWeight={maxWeight}&applyGaussianSmoothing={query.ApplyGaussianSmoothing.ToString().ToLowerInvariant()}";
        return apiClient.GetAsync<IReadOnlyList<HeatmapPointDto>>(uri, cancellationToken);
    }

    public Task<IReadOnlyList<MovementFlowDto>> GetMovementFlowsAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<MovementFlowDto>>("api/analytics/movement-flows", cancellationToken);
    }

    public Task<IReadOnlyList<MovementFlowDto>> GetMovementFlowsAsync(MovementFlowQueryDto query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var eventTypeSegment = query.EventTypeFilter.HasValue
            ? $"&eventTypeFilter={query.EventTypeFilter.Value}"
            : string.Empty;
        var minimumUniqueSessions = query.MinimumUniqueSessions.ToString(CultureInfo.InvariantCulture);
        var uri = $"api/analytics/movement-flows?timeRange={query.TimeRange}{eventTypeSegment}&minimumUniqueSessions={minimumUniqueSessions}";
        return apiClient.GetAsync<IReadOnlyList<MovementFlowDto>>(uri, cancellationToken);
    }

    public Task<IReadOnlyList<TopPoiDto>> GetTopPoisAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<TopPoiDto>>("api/analytics/top-pois", cancellationToken);
    }

    public Task<IReadOnlyList<PoiAverageListenDto>> GetAverageListenByPoiAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<PoiAverageListenDto>>("api/analytics/average-listen", cancellationToken);
    }

    public Task<AudioPlayAnalyticsDto> GetAudioPlayAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<AudioPlayAnalyticsDto>("api/analytics/audio-plays", cancellationToken);
    }

    public Task UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PutAsync($"api/admin/users/{userId}/role", request, cancellationToken);
    }
}
