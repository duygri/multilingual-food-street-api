using NarrationApp.Shared.DTOs.Geofence;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public interface IGeofenceService
{
    Task<IReadOnlyList<GeofenceDto>> GetByPoiAsync(int poiId, CancellationToken cancellationToken = default);

    Task<GeofenceDto> UpdateAsync(
        Guid actorUserId,
        UserRole actorRole,
        int poiId,
        UpdateGeofenceRequest request,
        CancellationToken cancellationToken = default);
}
