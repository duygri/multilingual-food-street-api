using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Analytics;

public sealed class AnalyticsServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_returns_counts_and_top_pois()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var totalPois = await dbContext.Pois.CountAsync();
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

        Assert.Equal(totalPois, result.TotalPois);
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

    [Fact]
    public async Task GetAnalyticsSnapshotAsync_returns_event_counts_and_average_listen_duration()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var startOfCurrentMonthUtc = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var previousMonthUtc = startOfCurrentMonthUtc.AddDays(-1);
        dbContext.VisitEvents.AddRange(
            new VisitEvent
            {
                DeviceId = "device-snapshot-1",
                PoiId = poi.Id,
                EventType = EventType.GeofenceEnter,
                Source = "geofence",
                ListenDurationSeconds = 0,
                CreatedAt = DateTime.UtcNow
            },
            new VisitEvent
            {
                DeviceId = "device-snapshot-1b",
                PoiId = poi.Id,
                EventType = EventType.GeofenceEnter,
                Source = "geofence",
                ListenDurationSeconds = 0,
                CreatedAt = previousMonthUtc
            },
            new VisitEvent
            {
                DeviceId = "device-snapshot-2",
                PoiId = poi.Id,
                EventType = EventType.QrScan,
                Source = "qr",
                ListenDurationSeconds = 0,
                CreatedAt = DateTime.UtcNow
            },
            new VisitEvent
            {
                DeviceId = "device-snapshot-3",
                PoiId = poi.Id,
                EventType = EventType.AudioPlay,
                Source = "audio",
                ListenDurationSeconds = 120,
                CreatedAt = DateTime.UtcNow
            },
            new VisitEvent
            {
                DeviceId = "device-snapshot-4",
                PoiId = poi.Id,
                EventType = EventType.AudioPlay,
                Source = "audio",
                ListenDurationSeconds = 180,
                CreatedAt = DateTime.UtcNow
            });
        await dbContext.SaveChangesAsync();

        var sut = new AnalyticsService(dbContext);
        var result = await sut.GetAnalyticsSnapshotAsync();

        Assert.Equal(2, result.GeofenceTriggers);
        Assert.Equal(1, result.CurrentMonthGeofenceTriggers);
        Assert.Equal(2, result.AudioPlays);
        Assert.Equal(1, result.QrScans);
        Assert.Equal(150d, result.AverageListenDurationSeconds);
    }

    [Fact]
    public async Task GetMovementFlowsAsync_returns_only_routes_that_meet_anonymous_session_threshold()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var pois = await dbContext.Pois
            .OrderBy(item => item.Id)
            .Take(3)
            .ToArrayAsync();
        var start = DateTime.UtcNow;

        dbContext.VisitEvents.AddRange(
            BuildVisitEvent("flow-device-1-a", pois[0].Id, EventType.QrScan, start.AddMinutes(0)),
            BuildVisitEvent("flow-device-1-a", pois[1].Id, EventType.AudioPlay, start.AddMinutes(3)),
            BuildVisitEvent("flow-device-1-b", pois[0].Id, EventType.QrScan, start.AddMinutes(10)),
            BuildVisitEvent("flow-device-1-b", pois[1].Id, EventType.AudioPlay, start.AddMinutes(13)),
            BuildVisitEvent("flow-device-1-c", pois[0].Id, EventType.QrScan, start.AddMinutes(20)),
            BuildVisitEvent("flow-device-1-c", pois[1].Id, EventType.AudioPlay, start.AddMinutes(23)),
            BuildVisitEvent("flow-device-2-a", pois[1].Id, EventType.QrScan, start.AddMinutes(30)),
            BuildVisitEvent("flow-device-2-a", pois[2].Id, EventType.AudioPlay, start.AddMinutes(33)),
            BuildVisitEvent("flow-device-2-b", pois[1].Id, EventType.QrScan, start.AddMinutes(40)),
            BuildVisitEvent("flow-device-2-b", pois[2].Id, EventType.AudioPlay, start.AddMinutes(43)));
        await dbContext.SaveChangesAsync();

        var sut = new AnalyticsService(dbContext);
        var result = await sut.GetMovementFlowsAsync();

        var flow = Assert.Single(result);
        Assert.Equal(pois[0].Id, flow.FromPoiId);
        Assert.Equal(pois[1].Id, flow.ToPoiId);
        Assert.Equal(3, flow.Weight);
        Assert.Equal(3, flow.UniqueSessions);
        Assert.Equal(pois[0].Name, flow.FromPoiName);
        Assert.Equal(pois[1].Name, flow.ToPoiName);
    }

    [Fact]
    public async Task GetMovementFlowsAsync_applies_time_event_and_anonymity_filters()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var pois = await dbContext.Pois
            .OrderBy(item => item.Id)
            .Take(2)
            .ToArrayAsync();
        var referenceTimeUtc = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc);

        dbContext.VisitEvents.AddRange(
            BuildVisitEvent("flow-filter-a", pois[0].Id, EventType.QrScan, referenceTimeUtc.AddDays(-1), pois[0].Lat, pois[0].Lng),
            BuildVisitEvent("flow-filter-a", pois[1].Id, EventType.QrScan, referenceTimeUtc.AddDays(-1).AddMinutes(4), pois[1].Lat, pois[1].Lng),
            BuildVisitEvent("flow-filter-b", pois[0].Id, EventType.QrScan, referenceTimeUtc.AddDays(-2), pois[0].Lat, pois[0].Lng),
            BuildVisitEvent("flow-filter-b", pois[1].Id, EventType.QrScan, referenceTimeUtc.AddDays(-2).AddMinutes(4), pois[1].Lat, pois[1].Lng),
            BuildVisitEvent("flow-filter-old", pois[0].Id, EventType.QrScan, referenceTimeUtc.AddDays(-10), pois[0].Lat, pois[0].Lng),
            BuildVisitEvent("flow-filter-old", pois[1].Id, EventType.QrScan, referenceTimeUtc.AddDays(-10).AddMinutes(4), pois[1].Lat, pois[1].Lng),
            BuildVisitEvent("flow-filter-audio", pois[0].Id, EventType.AudioPlay, referenceTimeUtc.AddDays(-1).AddHours(1), pois[0].Lat, pois[0].Lng),
            BuildVisitEvent("flow-filter-audio", pois[1].Id, EventType.AudioPlay, referenceTimeUtc.AddDays(-1).AddHours(1).AddMinutes(4), pois[1].Lat, pois[1].Lng));
        await dbContext.SaveChangesAsync();

        var sut = new AnalyticsService(dbContext);
        var result = await sut.GetMovementFlowsAsync(new MovementFlowQueryDto
        {
            TimeRange = HeatmapTimeRange.Last7Days,
            EventTypeFilter = EventType.QrScan,
            MinimumUniqueSessions = 2,
            ReferenceTimeUtc = referenceTimeUtc
        });

        var flow = Assert.Single(result);
        Assert.Equal(pois[0].Id, flow.FromPoiId);
        Assert.Equal(pois[1].Id, flow.ToPoiId);
        Assert.Equal(2, flow.Weight);
        Assert.Equal(2, flow.UniqueSessions);
    }

    [Fact]
    public async Task GetAverageListenByPoiAsync_returns_ranked_average_durations()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var pois = await dbContext.Pois
            .OrderBy(item => item.Id)
            .Take(2)
            .ToArrayAsync();
        var start = DateTime.UtcNow;

        dbContext.VisitEvents.AddRange(
            BuildVisitEvent("avg-device-1", pois[0].Id, EventType.AudioPlay, start.AddMinutes(0), listenDurationSeconds: 180),
            BuildVisitEvent("avg-device-2", pois[0].Id, EventType.AudioPlay, start.AddMinutes(2), listenDurationSeconds: 120),
            BuildVisitEvent("avg-device-3", pois[1].Id, EventType.AudioPlay, start.AddMinutes(4), listenDurationSeconds: 90),
            BuildVisitEvent("avg-device-4", pois[1].Id, EventType.AudioPlay, start.AddMinutes(6), listenDurationSeconds: 30));
        await dbContext.SaveChangesAsync();

        var sut = new AnalyticsService(dbContext);
        var result = await sut.GetAverageListenByPoiAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(pois[0].Id, result[0].PoiId);
        Assert.Equal(150d, result[0].AverageListenDurationSeconds);
        Assert.Equal(2, result[0].AudioPlayCount);
        Assert.Equal(pois[1].Id, result[1].PoiId);
        Assert.Equal(60d, result[1].AverageListenDurationSeconds);
    }

    [Fact]
    public async Task GetHeatmapAsync_snaps_nearby_points_to_same_grid_cell_and_counts_unique_sessions()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.OrderBy(item => item.Id).FirstAsync();
        var referenceTimeUtc = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc);

        dbContext.VisitEvents.AddRange(
            BuildVisitEvent("grid-device-a", poi.Id, EventType.GeofenceEnter, referenceTimeUtc.AddHours(-6), poi.Lat, poi.Lng),
            BuildVisitEvent("grid-device-a", poi.Id, EventType.AudioPlay, referenceTimeUtc.AddHours(-5).AddMinutes(-57), poi.Lat + 0.00002, poi.Lng + 0.00002),
            BuildVisitEvent("grid-device-a", poi.Id, EventType.QrScan, referenceTimeUtc.AddHours(-5).AddMinutes(-15), poi.Lat + 0.00003, poi.Lng + 0.00001),
            BuildVisitEvent("grid-device-b", poi.Id, EventType.GeofenceEnter, referenceTimeUtc.AddHours(-4), poi.Lat + 0.00001, poi.Lng + 0.00001));
        await dbContext.SaveChangesAsync();

        var sut = new AnalyticsService(dbContext);
        var result = await sut.GetHeatmapAsync(new HeatmapQueryDto
        {
            TimeRange = HeatmapTimeRange.AllTime,
            UseTimeDecay = false,
            GridSizeMeters = 50d,
            MaxWeight = 50d,
            ApplyGaussianSmoothing = false,
            ReferenceTimeUtc = referenceTimeUtc
        });

        var point = Assert.Single(result);
        Assert.Equal(3d, point.Weight, 6);
    }

    [Fact]
    public async Task GetHeatmapAsync_filters_events_outside_selected_time_range()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.OrderBy(item => item.Id).FirstAsync();
        var referenceTimeUtc = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc);

        dbContext.VisitEvents.AddRange(
            BuildVisitEvent("range-device-recent", poi.Id, EventType.GeofenceEnter, referenceTimeUtc.AddDays(-2), poi.Lat, poi.Lng),
            BuildVisitEvent("range-device-old", poi.Id, EventType.GeofenceEnter, referenceTimeUtc.AddDays(-10), poi.Lat, poi.Lng));
        await dbContext.SaveChangesAsync();

        var sut = new AnalyticsService(dbContext);
        var result = await sut.GetHeatmapAsync(new HeatmapQueryDto
        {
            TimeRange = HeatmapTimeRange.Last7Days,
            UseTimeDecay = false,
            GridSizeMeters = 50d,
            MaxWeight = 50d,
            ApplyGaussianSmoothing = false,
            ReferenceTimeUtc = referenceTimeUtc
        });

        var point = Assert.Single(result);
        Assert.Equal(1d, point.Weight, 6);
    }

    [Fact]
    public async Task GetHeatmapAsync_applies_time_decay_per_unique_session()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.OrderBy(item => item.Id).FirstAsync();
        var referenceTimeUtc = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc);

        dbContext.VisitEvents.AddRange(
            BuildVisitEvent("decay-device-recent", poi.Id, EventType.GeofenceEnter, referenceTimeUtc.AddDays(-1), poi.Lat, poi.Lng),
            BuildVisitEvent("decay-device-older", poi.Id, EventType.GeofenceEnter, referenceTimeUtc.AddDays(-16), poi.Lat, poi.Lng));
        await dbContext.SaveChangesAsync();

        var sut = new AnalyticsService(dbContext);
        var result = await sut.GetHeatmapAsync(new HeatmapQueryDto
        {
            TimeRange = HeatmapTimeRange.Last30Days,
            UseTimeDecay = true,
            GridSizeMeters = 50d,
            MaxWeight = 50d,
            ApplyGaussianSmoothing = false,
            ReferenceTimeUtc = referenceTimeUtc
        });

        var point = Assert.Single(result);
        var expected = Math.Exp(-1d / 15d) + Math.Exp(-16d / 15d);
        Assert.InRange(point.Weight, expected - 0.001d, expected + 0.001d);
    }

    [Fact]
    public async Task GetHeatmapAsync_filters_by_event_type()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.OrderBy(item => item.Id).FirstAsync();
        var referenceTimeUtc = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc);

        dbContext.VisitEvents.AddRange(
            BuildVisitEvent("event-filter-a", poi.Id, EventType.GeofenceEnter, referenceTimeUtc.AddHours(-3), poi.Lat, poi.Lng),
            BuildVisitEvent("event-filter-b", poi.Id, EventType.QrScan, referenceTimeUtc.AddHours(-2), poi.Lat, poi.Lng),
            BuildVisitEvent("event-filter-c", poi.Id, EventType.AudioPlay, referenceTimeUtc.AddHours(-1), poi.Lat, poi.Lng));
        await dbContext.SaveChangesAsync();

        var sut = new AnalyticsService(dbContext);
        var result = await sut.GetHeatmapAsync(new HeatmapQueryDto
        {
            TimeRange = HeatmapTimeRange.AllTime,
            EventTypeFilter = EventType.QrScan,
            UseTimeDecay = false,
            GridSizeMeters = 50d,
            MaxWeight = 50d,
            ApplyGaussianSmoothing = false,
            ReferenceTimeUtc = referenceTimeUtc
        });

        var point = Assert.Single(result);
        Assert.Equal(1d, point.Weight, 6);
    }

    [Fact]
    public async Task GetHeatmapAsync_clamps_outlier_weight()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.OrderBy(item => item.Id).FirstAsync();
        var referenceTimeUtc = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc);

        dbContext.VisitEvents.AddRange(
            BuildVisitEvent("clamp-device-1", poi.Id, EventType.GeofenceEnter, referenceTimeUtc.AddHours(-5), poi.Lat, poi.Lng),
            BuildVisitEvent("clamp-device-2", poi.Id, EventType.GeofenceEnter, referenceTimeUtc.AddHours(-4), poi.Lat, poi.Lng),
            BuildVisitEvent("clamp-device-3", poi.Id, EventType.GeofenceEnter, referenceTimeUtc.AddHours(-3), poi.Lat, poi.Lng),
            BuildVisitEvent("clamp-device-4", poi.Id, EventType.GeofenceEnter, referenceTimeUtc.AddHours(-2), poi.Lat, poi.Lng),
            BuildVisitEvent("clamp-device-5", poi.Id, EventType.GeofenceEnter, referenceTimeUtc.AddHours(-1), poi.Lat, poi.Lng));
        await dbContext.SaveChangesAsync();

        var sut = new AnalyticsService(dbContext);
        var result = await sut.GetHeatmapAsync(new HeatmapQueryDto
        {
            TimeRange = HeatmapTimeRange.AllTime,
            UseTimeDecay = false,
            GridSizeMeters = 50d,
            MaxWeight = 3d,
            ApplyGaussianSmoothing = false,
            ReferenceTimeUtc = referenceTimeUtc
        });

        var point = Assert.Single(result);
        Assert.Equal(3d, point.Weight, 6);
    }

    [Fact]
    public async Task GetHeatmapAsync_applies_gaussian_smoothing()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.OrderBy(item => item.Id).FirstAsync();
        var referenceTimeUtc = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc);

        dbContext.VisitEvents.Add(
            BuildVisitEvent("smooth-device-1", poi.Id, EventType.GeofenceEnter, referenceTimeUtc.AddHours(-1), poi.Lat, poi.Lng));
        await dbContext.SaveChangesAsync();

        var sut = new AnalyticsService(dbContext);
        var result = await sut.GetHeatmapAsync(new HeatmapQueryDto
        {
            TimeRange = HeatmapTimeRange.AllTime,
            UseTimeDecay = false,
            GridSizeMeters = 50d,
            MaxWeight = 50d,
            ApplyGaussianSmoothing = true,
            ReferenceTimeUtc = referenceTimeUtc
        });

        Assert.True(result.Count > 1);
        Assert.InRange(result.Max(item => item.Weight), 0.05d, 1d);
    }

    private static VisitEvent BuildVisitEvent(
        string deviceId,
        int poiId,
        EventType eventType,
        DateTime createdAt,
        double? lat = null,
        double? lng = null,
        int listenDurationSeconds = 0)
    {
        return new VisitEvent
        {
            DeviceId = deviceId,
            PoiId = poiId,
            EventType = eventType,
            Source = eventType == EventType.AudioPlay ? "audio" : "visit",
            ListenDurationSeconds = listenDurationSeconds,
            CreatedAt = createdAt,
            Lat = lat,
            Lng = lng
        };
    }
}
