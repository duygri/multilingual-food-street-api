using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.DTOs.Visitor;

namespace NarrationApp.Server.Tests.Services.VisitorNotifications;

public sealed class VisitorNotificationServiceTests
{
    [Fact]
    public async Task GetAsync_returns_nearby_poi_notification_when_location_is_inside_geofence()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois
            .Include(item => item.Geofences)
            .FirstAsync(item => item.Geofences.Any());
        var sut = new VisitorNotificationService(dbContext);

        var result = await sut.GetAsync(new VisitorNotificationsQuery
        {
            Lat = poi.Lat,
            Lng = poi.Lng,
            LanguageCode = "vi"
        });

        Assert.Contains(result, item => item.Title == $"Đang ở gần {poi.Name}");
    }

    [Fact]
    public async Task GetAsync_returns_live_tour_language_and_content_notifications()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = new VisitorNotificationService(dbContext);

        var result = await sut.GetAsync(new VisitorNotificationsQuery
        {
            LanguageCode = "en"
        });

        Assert.Contains(result, item => item.Title == "Tour mới vừa mở");
        Assert.Contains(result, item => item.Title == "Ngôn ngữ English sẵn sàng");
        Assert.Contains(result, item => item.Title == "Dữ liệu Vĩnh Khánh đã sẵn sàng");
    }
}
