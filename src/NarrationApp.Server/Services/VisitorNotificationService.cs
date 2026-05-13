using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.Visitor;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class VisitorNotificationService(AppDbContext dbContext) : IVisitorNotificationService
{
    private const int DefaultTake = 12;
    private const int MaxTake = 20;

    public async Task<IReadOnlyList<VisitorNotificationDto>> GetAsync(
        VisitorNotificationsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var take = Math.Clamp(query.Take <= 0 ? DefaultTake : query.Take, 1, MaxTake);
        var now = DateTime.UtcNow;
        var notifications = new List<VisitorNotificationDto>(take);

        await AddNearbyNotificationAsync(query, notifications, now, cancellationToken);
        await AddTourNotificationAsync(notifications, now, cancellationToken);
        await AddLanguageNotificationAsync(query.LanguageCode, notifications, now, cancellationToken);
        await AddContentReadyNotificationAsync(notifications, now, cancellationToken);

        return notifications
            .Take(take)
            .ToArray();
    }

    private async Task AddNearbyNotificationAsync(
        VisitorNotificationsQuery query,
        ICollection<VisitorNotificationDto> notifications,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (query.Lat is null || query.Lng is null || !IsValidCoordinate(query.Lat.Value, query.Lng.Value))
        {
            return;
        }

        var pois = await dbContext.Pois
            .AsNoTracking()
            .Include(poi => poi.Geofences)
            .Where(poi => poi.Status == PoiStatus.Published || poi.Status == PoiStatus.Updated)
            .Where(poi => poi.Geofences.Any(geofence => geofence.IsActive))
            .ToArrayAsync(cancellationToken);

        var nearby = pois
            .Select(poi => BuildNearbyCandidate(poi, query.Lat.Value, query.Lng.Value))
            .Where(candidate => candidate is not null)
            .Cast<NearbyCandidate>()
            .OrderBy(candidate => candidate.DistanceMeters)
            .ThenByDescending(candidate => candidate.Priority)
            .FirstOrDefault();

        if (nearby is null)
        {
            return;
        }

        notifications.Add(new VisitorNotificationDto
        {
            Title = $"Đang ở gần {nearby.PoiName}",
            Body = $"Bạn đang trong bán kính {nearby.RadiusMeters}m, còn khoảng {nearby.DistanceMeters}m. Mở bản đồ để nghe thuyết minh phù hợp.",
            TimeLabel = "Vừa xong",
            CreatedAtUtc = now
        });
    }

    private async Task AddTourNotificationAsync(
        ICollection<VisitorNotificationDto> notifications,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var tour = await dbContext.Tours
            .AsNoTracking()
            .Include(item => item.Stops)
            .Where(item => item.Status == TourStatus.Published)
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (tour is null)
        {
            return;
        }

        notifications.Add(new VisitorNotificationDto
        {
            Title = "Tour mới vừa mở",
            Body = $"Khám phá {tour.Title} có {tour.Stops.Count} điểm dừng.",
            TimeLabel = "Live API",
            CreatedAtUtc = now.AddMinutes(-5)
        });
    }

    private async Task AddLanguageNotificationAsync(
        string? languageCode,
        ICollection<VisitorNotificationDto> notifications,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var normalizedLanguageCode = NormalizeLanguageCode(languageCode);
        var language = await dbContext.ManagedLanguages
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.Code == normalizedLanguageCode)
            .ThenBy(item => item.Code)
            .FirstOrDefaultAsync(cancellationToken);

        if (language is null)
        {
            return;
        }

        var readyAudioCount = await dbContext.AudioAssets
            .AsNoTracking()
            .CountAsync(
                item => item.Status == AudioStatus.Ready
                    && item.LanguageCode.ToLower() == language.Code.ToLower(),
                cancellationToken);

        var body = readyAudioCount > 0
            ? $"Có {readyAudioCount} audio thuyết minh cho ngôn ngữ {language.DisplayName}."
            : $"Ngôn ngữ {language.DisplayName} đang bật trong hệ thống. Bạn có thể đổi ở header bất kỳ lúc nào.";

        notifications.Add(new VisitorNotificationDto
        {
            Title = $"Ngôn ngữ {language.DisplayName} sẵn sàng",
            Body = body,
            TimeLabel = "Live API",
            CreatedAtUtc = now.AddMinutes(-10)
        });
    }

    private async Task AddContentReadyNotificationAsync(
        ICollection<VisitorNotificationDto> notifications,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var poiCount = await dbContext.Pois
            .AsNoTracking()
            .CountAsync(poi => poi.Status == PoiStatus.Published || poi.Status == PoiStatus.Updated, cancellationToken);

        if (poiCount <= 0)
        {
            return;
        }

        notifications.Add(new VisitorNotificationDto
        {
            Title = "Dữ liệu Vĩnh Khánh đã sẵn sàng",
            Body = $"Đã đồng bộ {poiCount} POI từ API thật để hiển thị bản đồ và khám phá.",
            TimeLabel = "Live API",
            CreatedAtUtc = now.AddMinutes(-15)
        });
    }

    private static NearbyCandidate? BuildNearbyCandidate(Poi poi, double latitude, double longitude)
    {
        var distanceMeters = CalculateDistanceMeters(latitude, longitude, poi.Lat, poi.Lng);
        var geofence = poi.Geofences
            .Where(item => item.IsActive && distanceMeters <= item.RadiusMeters)
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.RadiusMeters)
            .FirstOrDefault();

        if (geofence is null)
        {
            return null;
        }

        return new NearbyCandidate(
            poi.Name,
            (int)Math.Round(distanceMeters),
            geofence.RadiusMeters,
            geofence.Priority);
    }

    private static bool IsValidCoordinate(double latitude, double longitude)
    {
        return latitude is >= -90 and <= 90
            && longitude is >= -180 and <= 180;
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "vi"
            : languageCode.Trim().ToLowerInvariant();
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6_371_000d;
        var deltaLat = ToRadians(lat2 - lat1);
        var deltaLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)
            + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
            * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;

    private sealed record NearbyCandidate(string PoiName, int DistanceMeters, int RadiusMeters, int Priority);
}
