using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.VisitEvents;

public sealed class VisitEventServiceTests
{
    [Fact]
    public async Task CreateAsync_persists_anonymous_visit_event()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var sut = new VisitEventService(dbContext);

        await sut.CreateAsync(new VisitEventService.CreateVisitEventRequest
        {
            DeviceId = "device-visit",
            PoiId = poi.Id,
            EventType = EventType.GeofenceEnter,
            Source = "geofence",
            ListenDurationSeconds = 0,
            Lat = poi.Lat,
            Lng = poi.Lng
        });

        Assert.Equal(1, await dbContext.VisitEvents.CountAsync(item => item.PoiId == poi.Id && item.DeviceId == "device-visit"));
    }
}
