using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Geofence;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class GeofenceService(AppDbContext dbContext) : IGeofenceService
{
    public async Task<IReadOnlyList<GeofenceDto>> GetByPoiAsync(int poiId, CancellationToken cancellationToken = default)
    {
        var geofences = await dbContext.Geofences
            .AsNoTracking()
            .Where(geofence => geofence.PoiId == poiId)
            .OrderByDescending(geofence => geofence.Priority)
            .ToListAsync(cancellationToken);

        return geofences.Select(geofence => geofence.ToDto()).ToArray();
    }

    public async Task<GeofenceDto> UpdateAsync(
        Guid actorUserId,
        UserRole actorRole,
        int poiId,
        UpdateGeofenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var poi = await dbContext.Pois.SingleOrDefaultAsync(poi => poi.Id == poiId, cancellationToken)
            ?? throw new KeyNotFoundException("POI was not found.");
        EnsureWriteAccess(poi, actorUserId, actorRole);

        var geofence = await dbContext.Geofences
            .SingleOrDefaultAsync(item => item.PoiId == poiId, cancellationToken);
        var canManagePriority = actorRole == UserRole.Admin;

        if (geofence is null)
        {
            geofence = new Geofence
            {
                PoiId = poiId,
                Name = request.Name,
                RadiusMeters = AppConstants.DefaultGeofenceRadiusMeters,
                Priority = canManagePriority ? request.Priority : AppConstants.DefaultPoiPriority,
                DebounceSeconds = AppConstants.DefaultDebounceSeconds,
                CooldownSeconds = AppConstants.DefaultCooldownSeconds,
                IsActive = true,
                TriggerAction = "auto_play",
                NearestOnly = true
            };

            dbContext.Geofences.Add(geofence);
        }

        geofence.Name = request.Name;
        geofence.RadiusMeters = request.RadiusMeters;
        if (canManagePriority)
        {
            geofence.Priority = request.Priority;
        }

        geofence.DebounceSeconds = request.DebounceSeconds;
        geofence.CooldownSeconds = request.CooldownSeconds;
        geofence.IsActive = request.IsActive;
        geofence.TriggerAction = request.TriggerAction;
        geofence.NearestOnly = request.NearestOnly;

        await dbContext.SaveChangesAsync(cancellationToken);
        return geofence.ToDto();
    }

    private static void EnsureWriteAccess(Poi poi, Guid actorUserId, UserRole actorRole)
    {
        if (actorRole == UserRole.Admin)
        {
            return;
        }

        if (actorRole != UserRole.PoiOwner || poi.OwnerId != actorUserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this geofence.");
        }
    }

    private static void ValidateRequest(UpdateGeofenceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Geofence name is required.", nameof(request));
        }

        if (request.RadiusMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.RadiusMeters), "Geofence radius must be greater than zero.");
        }

        if (request.DebounceSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.DebounceSeconds), "Debounce must be zero or greater.");
        }

        if (request.CooldownSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.CooldownSeconds), "Cooldown must be zero or greater.");
        }
    }
}
