using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.DTOs.Geofence;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Geofences;

public sealed class GeofenceServiceTests
{
    [Fact]
    public async Task UpdateAsync_updates_existing_geofence_values()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var geofence = await dbContext.Geofences.FirstAsync();
        var sut = new GeofenceService(dbContext);

        var result = await sut.UpdateAsync(
            Guid.NewGuid(),
            UserRole.Admin,
            geofence.PoiId,
            new UpdateGeofenceRequest
            {
                Name = "Vung moi",
                RadiusMeters = 45,
                Priority = 7,
                DebounceSeconds = 15,
                CooldownSeconds = 900,
                IsActive = true,
                TriggerAction = "notify_only",
                NearestOnly = false
            });

        Assert.Equal("Vung moi", result.Name);
        Assert.Equal(45, result.RadiusMeters);
        Assert.Equal(7, result.Priority);
        Assert.Equal(900, result.CooldownSeconds);
        Assert.False(result.NearestOnly);
    }

    [Fact]
    public async Task UpdateAsync_preserves_priority_when_owner_updates_radius()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var geofence = await dbContext.Geofences.Include(item => item.Poi).FirstAsync();
        var originalPriority = geofence.Priority;
        var sut = new GeofenceService(dbContext);

        var result = await sut.UpdateAsync(
            geofence.Poi!.OwnerId,
            UserRole.PoiOwner,
            geofence.PoiId,
            new UpdateGeofenceRequest
            {
                Name = "Vung owner",
                RadiusMeters = 55,
                Priority = originalPriority + 20,
                DebounceSeconds = 8,
                CooldownSeconds = 240,
                IsActive = true,
                TriggerAction = "auto_play",
                NearestOnly = true
            });

        var persistedGeofence = await dbContext.Geofences.SingleAsync(item => item.Id == geofence.Id);

        Assert.Equal(55, result.RadiusMeters);
        Assert.Equal(originalPriority, result.Priority);
        Assert.Equal(originalPriority, persistedGeofence.Priority);
    }

    [Fact]
    public async Task UpdateAsync_rejects_owner_attempting_to_edit_foreign_geofence()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var foreignOwner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-geofence-foreign@narration.app");
        var geofence = await dbContext.Geofences.AsNoTracking().FirstAsync();
        var sut = new GeofenceService(dbContext);

        var action = async () => await sut.UpdateAsync(
            foreignOwner.Id,
            UserRole.PoiOwner,
            geofence.PoiId,
            new UpdateGeofenceRequest
            {
                Name = "Vung foreign",
                RadiusMeters = 55,
                Priority = geofence.Priority + 20,
                DebounceSeconds = 8,
                CooldownSeconds = 240,
                IsActive = true,
                TriggerAction = "auto_play",
                NearestOnly = true
            });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
    }

    [Fact]
    public async Task UpdateAsync_throws_when_radius_is_not_positive()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var geofence = await dbContext.Geofences.FirstAsync();
        var sut = new GeofenceService(dbContext);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.UpdateAsync(
            Guid.NewGuid(),
            UserRole.Admin,
            geofence.PoiId,
            new UpdateGeofenceRequest
            {
                Name = "Vung loi",
                RadiusMeters = 0,
                Priority = 5,
                DebounceSeconds = 10,
                CooldownSeconds = 600,
                IsActive = true,
                TriggerAction = "auto_play",
                NearestOnly = true
            }));
    }
}
