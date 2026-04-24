using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;

namespace NarrationApp.Web.Services;

public interface IOwnerPortalService
{
    Task<OwnerShellSummaryDto> GetShellSummaryAsync(CancellationToken cancellationToken = default);

    Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<OwnerDashboardWorkspaceDto> GetDashboardWorkspaceAsync(CancellationToken cancellationToken = default)
        => Task.FromException<OwnerDashboardWorkspaceDto>(new NotSupportedException());

    Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default);

    Task<OwnerPoisWorkspaceDto> GetPoisWorkspaceAsync(CancellationToken cancellationToken = default)
        => Task.FromException<OwnerPoisWorkspaceDto>(new NotSupportedException());

    Task<PoiDto> GetPoiAsync(int poiId, CancellationToken cancellationToken = default);

    Task<OwnerPoiDetailWorkspaceDto> GetPoiWorkspaceAsync(int poiId, CancellationToken cancellationToken = default)
        => Task.FromException<OwnerPoiDetailWorkspaceDto>(new NotSupportedException());

    Task<OwnerPoiStatsDto> GetPoiStatsAsync(int poiId, CancellationToken cancellationToken = default);

    Task<OwnerModerationWorkspaceDto> GetModerationWorkspaceAsync(CancellationToken cancellationToken = default)
        => Task.FromException<OwnerModerationWorkspaceDto>(new NotSupportedException());

    Task<PoiDto> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default);

    Task<PoiDto> UpdatePoiAsync(int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default);

    Task<PoiDto> UploadPoiImageAsync(
        int poiId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
        => Task.FromException<PoiDto>(new NotSupportedException());

    Task<PoiDto> DeletePoiImageAsync(int poiId, CancellationToken cancellationToken = default)
        => Task.FromException<PoiDto>(new NotSupportedException());

    Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default);
}
