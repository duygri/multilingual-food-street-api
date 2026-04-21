using NarrationApp.Shared.DTOs.Geofence;

namespace NarrationApp.Web.Services;

public interface IGeofencePortalService
{
    Task<GeofenceDto> UpdateAsync(int poiId, UpdateGeofenceRequest request, CancellationToken cancellationToken = default);
}
