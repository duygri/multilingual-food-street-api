using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;

namespace NarrationApp.Web.Services;

public interface IOwnerPortalService
{
    Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default);

    Task<PoiDto> GetPoiAsync(int poiId, CancellationToken cancellationToken = default);

    Task<OwnerPoiStatsDto> GetPoiStatsAsync(int poiId, CancellationToken cancellationToken = default);

    Task<PoiDto> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default);

    Task<PoiDto> UpdatePoiAsync(int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default);

    Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default);
}
