using NarrationApp.Shared.DTOs.Geofence;

namespace NarrationApp.Web.Services;

public sealed class GeofencePortalService(ApiClient apiClient) : IGeofencePortalService
{
    public Task<GeofenceDto> UpdateAsync(int poiId, UpdateGeofenceRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PutAsync<UpdateGeofenceRequest, GeofenceDto>($"api/geofences/{poiId}", request, cancellationToken);
    }
}
