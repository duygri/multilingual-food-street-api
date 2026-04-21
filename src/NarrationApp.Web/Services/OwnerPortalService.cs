using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;

namespace NarrationApp.Web.Services;

public sealed class OwnerPortalService(ApiClient apiClient) : IOwnerPortalService
{
    public Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<OwnerDashboardDto>("api/owner/dashboard", cancellationToken);
    }

    public Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<PoiDto>>("api/owner/pois", cancellationToken);
    }

    public Task<OwnerPoiStatsDto> GetPoiStatsAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<OwnerPoiStatsDto>($"api/owner/pois/{poiId}/stats", cancellationToken);
    }

    public Task<PoiDto> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<CreatePoiRequest, PoiDto>("api/pois", request, cancellationToken);
    }

    public Task<PoiDto> UpdatePoiAsync(int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PutAsync<UpdatePoiRequest, PoiDto>($"api/pois/{poiId}", request, cancellationToken);
    }

    public Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return apiClient.DeleteAsync($"api/pois/{poiId}", cancellationToken);
    }
}
