using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Tour;

namespace NarrationApp.Web.Services;

public sealed class TourPortalService(ApiClient apiClient) : ITourPortalService
{
    public Task<IReadOnlyList<TourDto>> GetToursAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<TourDto>>("api/tours", cancellationToken);
    }

    public Task<IReadOnlyList<PoiDto>> GetPoiOptionsAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<PoiDto>>("api/pois", cancellationToken);
    }

    public Task<TourDto> CreateTourAsync(CreateTourRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<CreateTourRequest, TourDto>("api/tours", request, cancellationToken);
    }

    public Task<TourDto> UpdateTourAsync(int id, UpdateTourRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PutAsync<UpdateTourRequest, TourDto>($"api/tours/{id}", request, cancellationToken);
    }

    public Task DeleteTourAsync(int id, CancellationToken cancellationToken = default)
    {
        return apiClient.DeleteAsync($"api/tours/{id}", cancellationToken);
    }
}
