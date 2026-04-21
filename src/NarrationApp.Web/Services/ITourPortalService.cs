using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Tour;

namespace NarrationApp.Web.Services;

public interface ITourPortalService
{
    Task<IReadOnlyList<TourDto>> GetToursAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PoiDto>> GetPoiOptionsAsync(CancellationToken cancellationToken = default);

    Task<TourDto> CreateTourAsync(CreateTourRequest request, CancellationToken cancellationToken = default);

    Task<TourDto> UpdateTourAsync(int id, UpdateTourRequest request, CancellationToken cancellationToken = default);

    Task DeleteTourAsync(int id, CancellationToken cancellationToken = default);
}
