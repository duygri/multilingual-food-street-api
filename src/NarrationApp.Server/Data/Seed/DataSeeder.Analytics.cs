using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Data.Seed;

public sealed partial class DataSeeder
{
    private const string SeedAnalyticsDevicePrefix = "seed-admin-map-";

    private async Task SeedAnalyticsVisitEventsAsync(CancellationToken cancellationToken)
    {
        var hasSeedAnalyticsEvents = await dbContext.VisitEvents
            .AnyAsync(item => item.DeviceId.StartsWith(SeedAnalyticsDevicePrefix), cancellationToken);

        if (hasSeedAnalyticsEvents)
        {
            return;
        }

        var requiredSlugs = SeedAnalyticsRoutes
            .SelectMany(route => route.StopSlugs)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var poisBySlug = await dbContext.Pois
            .Where(poi => requiredSlugs.Contains(poi.Slug))
            .ToDictionaryAsync(poi => poi.Slug, cancellationToken);

        if (requiredSlugs.Any(slug => !poisBySlug.ContainsKey(slug)))
        {
            logger.LogWarning("Skipped analytics sample seed because one or more POIs are missing.");
            return;
        }

        var now = DateTime.UtcNow;
        var events = new List<VisitEvent>();

        for (var routeIndex = 0; routeIndex < SeedAnalyticsRoutes.Count; routeIndex++)
        {
            var route = SeedAnalyticsRoutes[routeIndex];

            for (var sessionIndex = 0; sessionIndex < route.SessionCount; sessionIndex++)
            {
                var deviceId = $"{SeedAnalyticsDevicePrefix}{route.Key}-{sessionIndex + 1:00}";
                var sessionStartUtc = now
                    .AddDays(-((sessionIndex + routeIndex) % 6))
                    .AddHours(-2 - routeIndex)
                    .AddMinutes(sessionIndex * 9);

                for (var stopIndex = 0; stopIndex < route.StopSlugs.Count; stopIndex++)
                {
                    var poi = poisBySlug[route.StopSlugs[stopIndex]];
                    var stopSeenAtUtc = sessionStartUtc.AddMinutes(stopIndex * 7);
                    var jitter = (sessionIndex - (route.SessionCount / 2d)) * 0.000035d;
                    var lat = poi.Lat + jitter + (stopIndex * 0.000012d);
                    var lng = poi.Lng - jitter + (stopIndex * 0.00001d);

                    if (stopIndex == 0)
                    {
                        events.Add(BuildSeedVisitEvent(deviceId, poi.Id, EventType.QrScan, stopSeenAtUtc.AddSeconds(-35), lat, lng));
                    }

                    events.Add(BuildSeedVisitEvent(deviceId, poi.Id, EventType.GeofenceEnter, stopSeenAtUtc, lat, lng));
                    events.Add(BuildSeedVisitEvent(deviceId, poi.Id, EventType.AudioPlay, stopSeenAtUtc.AddSeconds(70), lat, lng, 55 + (stopIndex * 18) + (sessionIndex * 4)));
                    events.Add(BuildSeedVisitEvent(deviceId, poi.Id, EventType.TourProgress, stopSeenAtUtc.AddSeconds(120), lat, lng));
                }
            }
        }

        dbContext.VisitEvents.AddRange(events);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} analytics sample visit events for admin heatmap and anonymous movement flows.", events.Count);
    }

    private static VisitEvent BuildSeedVisitEvent(
        string deviceId,
        int poiId,
        EventType eventType,
        DateTime createdAtUtc,
        double lat,
        double lng,
        int listenDurationSeconds = 0)
    {
        return new VisitEvent
        {
            DeviceId = deviceId,
            PoiId = poiId,
            EventType = eventType,
            Source = eventType switch
            {
                EventType.AudioPlay => "audio",
                EventType.GeofenceEnter => "geofence",
                EventType.QrScan => "qr",
                EventType.TourProgress => "tour",
                _ => "analytics"
            },
            ListenDurationSeconds = listenDurationSeconds,
            Lat = lat,
            Lng = lng,
            CreatedAt = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc)
        };
    }

    private static IReadOnlyList<SeedAnalyticsRouteDefinition> SeedAnalyticsRoutes { get; } =
    [
        new(
            "seafood",
            4,
            [
                "oc-oanh-vinh-khanh",
                "oc-sau-no-vinh-khanh",
                "oc-thao-vinh-khanh",
                "oc-dao-vinh-khanh"
            ]),
        new(
            "bus-food",
            3,
            [
                "bun-thit-nuong-co-nga",
                "bun-ca-chau-doc-di-tu",
                "lang-quan-vinh-khanh",
                "ot-xiem-quan-vinh-khanh"
            ]),
        new(
            "grill",
            3,
            [
                "chili-lau-nuong-tu-chon",
                "quan-hoa-vinh-khanh",
                "an-an-quan-vinh-khanh"
            ])
    ];

    private sealed record SeedAnalyticsRouteDefinition(
        string Key,
        int SessionCount,
        IReadOnlyList<string> StopSlugs);
}
