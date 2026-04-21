using NarrationApp.Shared.DTOs.Geofence;

namespace NarrationApp.Server.Services;

public interface IGeofenceService
{
    Task<IReadOnlyList<GeofenceDto>> GetByPoiAsync(int poiId, CancellationToken cancellationToken = default);

    Task<GeofenceDto> UpdateAsync(int poiId, UpdateGeofenceRequest request, CancellationToken cancellationToken = default);
}
