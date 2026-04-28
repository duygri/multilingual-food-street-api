using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Analytics;

public sealed class VisitEventRetentionServiceTests
{
    [Fact]
    public async Task PurgeExpiredAsync_deletes_only_visit_events_older_than_retention_window()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = dbContext.Pois.OrderBy(item => item.Id).First();
        var referenceTimeUtc = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc);
        var cutoffUtc = referenceTimeUtc.AddDays(-30);

        dbContext.VisitEvents.AddRange(
            BuildVisitEvent("retention-old-a", poi.Id, cutoffUtc.AddSeconds(-1)),
            BuildVisitEvent("retention-old-b", poi.Id, cutoffUtc.AddDays(-5)),
            BuildVisitEvent("retention-cutoff", poi.Id, cutoffUtc),
            BuildVisitEvent("retention-recent", poi.Id, referenceTimeUtc.AddDays(-1)));
        await dbContext.SaveChangesAsync();

        var sut = new VisitEventRetentionService(
            dbContext,
            Options.Create(new VisitEventRetentionOptions
            {
                RawEventRetentionDays = 30,
                BatchSize = 2
            }),
            NullLogger<VisitEventRetentionService>.Instance);

        var deleted = await sut.PurgeExpiredAsync(referenceTimeUtc);
        var remainingDeviceIds = dbContext.VisitEvents
            .Select(item => item.DeviceId)
            .ToArray();

        Assert.Equal(2, deleted);
        Assert.DoesNotContain("retention-old-a", remainingDeviceIds);
        Assert.DoesNotContain("retention-old-b", remainingDeviceIds);
        Assert.Contains("retention-cutoff", remainingDeviceIds);
        Assert.Contains("retention-recent", remainingDeviceIds);
    }

    private static VisitEvent BuildVisitEvent(string deviceId, int poiId, DateTime createdAt)
    {
        return new VisitEvent
        {
            DeviceId = deviceId,
            PoiId = poiId,
            EventType = EventType.GeofenceEnter,
            Source = "retention-test",
            CreatedAt = createdAt,
            Lat = 10.75,
            Lng = 106.7
        };
    }
}
