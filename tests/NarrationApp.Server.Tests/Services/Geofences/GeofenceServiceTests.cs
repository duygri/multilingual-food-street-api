using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.DTOs.Geofence;

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
        Assert.Equal(900, result.CooldownSeconds);
        Assert.False(result.NearestOnly);
    }

    [Fact]
    public async Task UpdateAsync_throws_when_radius_is_not_positive()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var geofence = await dbContext.Geofences.FirstAsync();
        var sut = new GeofenceService(dbContext);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.UpdateAsync(
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
