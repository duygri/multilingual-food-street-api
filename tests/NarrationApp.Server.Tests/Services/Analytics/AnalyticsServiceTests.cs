using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Analytics;

public sealed class AnalyticsServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_returns_counts_and_top_pois()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        dbContext.VisitEvents.Add(new VisitEvent
        {
            DeviceId = "device-analytics",
            PoiId = poi.Id,
            EventType = EventType.AudioPlay,
            Source = "audio",
            ListenDurationSeconds = 30,
            CreatedAt = DateTime.UtcNow,
            Lat = poi.Lat,
            Lng = poi.Lng
        });
        await dbContext.SaveChangesAsync();

        var sut = new AnalyticsService(dbContext);
        var result = await sut.GetDashboardAsync();

        Assert.Equal(5, result.TotalPois);
        Assert.NotEmpty(result.TopPois);
        Assert.Equal(poi.Id, result.TopPois[0].PoiId);
    }

    [Fact]
    public async Task GetAudioPlayAnalyticsAsync_returns_total_plays_and_listen_seconds()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        dbContext.VisitEvents.AddRange(
            new VisitEvent
            {
                DeviceId = "device-analytics-1",
                PoiId = poi.Id,
                EventType = EventType.AudioPlay,
                Source = "audio",
                ListenDurationSeconds = 45,
                CreatedAt = DateTime.UtcNow
            },
            new VisitEvent
            {
                DeviceId = "device-analytics-2",
                PoiId = poi.Id,
                EventType = EventType.AudioPlay,
                Source = "audio",
                ListenDurationSeconds = 75,
                CreatedAt = DateTime.UtcNow
            });
        await dbContext.SaveChangesAsync();

        var sut = new AnalyticsService(dbContext);
        var result = await sut.GetAudioPlayAnalyticsAsync();

        Assert.Equal(2, result.TotalAudioPlays);
        Assert.Equal(120, result.TotalListenSeconds);
    }
}
