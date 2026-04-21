using System.Net.Http.Json;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Mobile.Features.Home;

public interface ITouristContentService
{
    Task<TouristContentResult> LoadAsync(TouristContentLoadRequest? request = null, CancellationToken cancellationToken = default);
}

public sealed class TouristContentService(HttpClient httpClient, ITouristLocationService locationService) : ITouristContentService
{
    public async Task<TouristContentResult> LoadAsync(TouristContentLoadRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new TouristContentLoadRequest();
        var location = await GetLocationSnapshotAsync(request, cancellationToken);

        try
        {
            var poisEndpoint = BuildPoisEndpoint(request, location);
            var poisResponse = await httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<PoiDto>>>(poisEndpoint, cancellationToken);
            var toursResponse = await httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<TourDto>>>("api/tours", cancellationToken);

            var pois = poisResponse?.Data ?? [];
            var tours = toursResponse?.Data ?? [];

            var snapshot = TouristContentMapper.Map(pois, tours);
            return new TouristContentResult(
                snapshot,
                IsFallback: false,
                SourceLabel: "Live API",
                Message: BuildSuccessMessage(snapshot, location),
                Location: location);
        }
        catch (Exception ex)
        {
            return new TouristContentResult(
                TouristContentSnapshot.CreateDemo(),
                IsFallback: true,
                SourceLabel: "Demo fallback",
                Message: $"Không chạm được API thật, tạm dùng dữ liệu demo. {ex.Message}",
                Location: location);
        }
    }

    private async Task<TouristLocationSnapshot> GetLocationSnapshotAsync(TouristContentLoadRequest request, CancellationToken cancellationToken)
    {
        if (!request.PreferNearbyPois && !request.RequestLocationPermission)
        {
            return TouristLocationSnapshot.Disabled();
        }

        try
        {
            return await locationService.GetCurrentAsync(request.RequestLocationPermission, cancellationToken);
        }
        catch (Exception ex)
        {
            return TouristLocationSnapshot.Disabled($"Không lấy được vị trí: {ex.Message}");
        }
    }

    private static string BuildPoisEndpoint(TouristContentLoadRequest request, TouristLocationSnapshot location)
    {
        if (!request.PreferNearbyPois
            || !location.PermissionGranted
            || !location.IsLocationAvailable
            || location.Latitude is null
            || location.Longitude is null)
        {
            return "api/pois";
        }

        var lat = location.Latitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lng = location.Longitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return $"api/pois/near?lat={lat}&lng={lng}&radiusMeters={request.RadiusMeters}";
    }

    private static string BuildSuccessMessage(TouristContentSnapshot snapshot, TouristLocationSnapshot location)
    {
        if (location.PermissionGranted && location.IsLocationAvailable)
        {
            return $"Đã tải {snapshot.Pois.Count} POI gần bạn và {snapshot.Tours.Count} tour từ máy chủ.";
        }

        return $"Đã tải {snapshot.Pois.Count} POI và {snapshot.Tours.Count} tour từ máy chủ.";
    }
}

public static class TouristContentMapper
{
    public static TouristContentSnapshot Map(IReadOnlyList<PoiDto> pois, IReadOnlyList<TourDto> tours)
    {
        var mappedPois = MapPois(pois);
        var mappedTours = tours
            .Where(tour => tour.Status == TourStatus.Published)
            .Select(MapTour)
            .ToList();

        return new TouristContentSnapshot(mappedPois, mappedTours);
    }

    private static IReadOnlyList<TouristPoi> MapPois(IReadOnlyList<PoiDto> pois)
    {
        var publishedPois = pois
            .Where(poi => poi.Status == PoiStatus.Published || poi.Status == PoiStatus.Updated)
            .OrderByDescending(poi => poi.Priority)
            .ThenBy(poi => poi.Id)
            .ToList();

        if (publishedPois.Count == 0)
        {
            return [];
        }

        var minLat = publishedPois.Min(poi => poi.Lat);
        var maxLat = publishedPois.Max(poi => poi.Lat);
        var minLng = publishedPois.Min(poi => poi.Lng);
        var maxLng = publishedPois.Max(poi => poi.Lng);

        return publishedPois
            .Select((poi, index) =>
            {
                var translationCount = poi.Translations
                    .Select(translation => translation.LanguageCode)
                    .Where(languageCode => !string.IsNullOrWhiteSpace(languageCode))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
                var geofenceRadiusMeters = Math.Max(30, poi.Geofences.FirstOrDefault()?.RadiusMeters ?? 30);
                var nearbyDistanceMeters = Math.Max(geofenceRadiusMeters + 75, 120 + index * 30);

                return new TouristPoi(
                    Id: $"poi-{poi.Id}",
                    Name: poi.Name,
                    CategoryId: ToCategoryId(poi),
                    CategoryLabel: BuildCategoryLabel(poi),
                    District: BuildAreaLabel(poi),
                    StoryTag: BuildStoryTag(poi),
                    Description: poi.Description,
                    Highlight: poi.Translations.FirstOrDefault(translation => !string.IsNullOrWhiteSpace(translation.Highlight))?.Highlight
                        ?? "Audio guide sẵn sàng",
                    MapTopPercent: Normalize(poi.Lat, minLat, maxLat, 18d, 74d, index),
                    MapLeftPercent: Normalize(poi.Lng, minLng, maxLng, 18d, 72d, index),
                    DistanceMeters: nearbyDistanceMeters,
                    AudioDuration: EstimateAudioDuration(poi),
                    StatusLabel: BuildStatusLabel(poi),
                    Latitude: poi.Lat,
                    Longitude: poi.Lng,
                    Priority: Math.Max(1, poi.Priority),
                    AvailableLanguageCount: Math.Max(1, translationCount + 1),
                    GeofenceRadiusMeters: geofenceRadiusMeters);
            })
            .ToList();
    }

    private static TouristTourCard MapTour(TourDto tour)
    {
        return new TouristTourCard(
            Id: $"tour-{tour.Id}",
            Title: tour.Title,
            StopCountLabel: $"{tour.Stops.Count} điểm dừng",
            DurationLabel: $"{tour.EstimatedMinutes} phút",
            DifficultyLabel: GetDifficultyLabel(tour.Stops.Count, tour.EstimatedMinutes),
            Description: string.IsNullOrWhiteSpace(tour.Description) ? "Tour sẵn sàng để bắt đầu." : tour.Description,
            StopPoiIds: tour.Stops
                .OrderBy(stop => stop.Sequence)
                .Select(stop => $"poi-{stop.PoiId}")
                .ToArray());
    }

    private static string BuildStoryTag(PoiDto poi)
    {
        var translationCount = poi.Translations
            .Select(translation => translation.LanguageCode)
            .Where(languageCode => !string.IsNullOrWhiteSpace(languageCode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return translationCount > 0
            ? $"Live API • {translationCount + 1} ngôn ngữ"
            : $"Live API • {GetNarrationLabel(poi.NarrationMode)}";
    }

    private static string BuildStatusLabel(PoiDto poi)
    {
        return poi.NarrationMode switch
        {
            NarrationMode.RecordedOnly => "Audio thu sẵn",
            NarrationMode.Both => "Audio + TTS",
            _ => "TTS sẵn sàng"
        };
    }

    private static string BuildCategoryLabel(PoiDto poi)
    {
        if (!string.IsNullOrWhiteSpace(poi.CategoryName))
        {
            return poi.CategoryName.Trim();
        }

        return ToCategoryId(poi) switch
        {
            "food" => "Ẩm thực",
            "night" => "Đêm",
            "river" => "Ven sông",
            _ => "Di tích"
        };
    }

    private static string BuildAreaLabel(PoiDto poi)
    {
        var normalized = $"{poi.Name} {poi.Slug}".ToLowerInvariant();

        if (normalized.Contains("khánh hội") || normalized.Contains("xóm chiếu"))
        {
            return "Q4, Xóm Chiếu";
        }

        if (normalized.Contains("bến thành") || normalized.Contains("đức bà") || normalized.Contains("nguyễn huệ"))
        {
            return "Q1, Trung tâm";
        }

        if (normalized.Contains("nhà rồng") || normalized.Contains("bến"))
        {
            return "Q4, Bến Nhà Rồng";
        }

        return poi.Lat < 10.7670 ? "Q4, Khánh Hội" : "Q1, Sài Gòn";
    }

    private static string ToCategoryId(PoiDto poi)
    {
        var normalized = $"{poi.CategoryName} {poi.Name} {poi.Slug}".Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "all";
        }

        return normalized switch
        {
            var value when value.Contains("ẩm") || value.Contains("bun") || value.Contains("phở") || value.Contains("food") || value.Contains("đồ uống") || value.Contains("uống") || value.Contains("cà phê") || value.Contains("coffee") => "food",
            var value when value.Contains("đêm") || value.Contains("night") => "night",
            var value when value.Contains("sông") || value.Contains("river") || value.Contains("bến") || value.Contains("cầu") || value.Contains("bridge") => "river",
            var value when value.Contains("di tích") || value.Contains("lịch sử") || value.Contains("tín ngưỡng") || value.Contains("chùa") || value.Contains("nhà thờ") || value.Contains("heritage") => "history",
            _ => "history"
        };
    }

    private static double Normalize(double value, double min, double max, double outputMin, double outputMax, int index)
    {
        if (Math.Abs(max - min) < 0.000001d)
        {
            return Math.Clamp(outputMin + index * 8d, outputMin, outputMax);
        }

        var ratio = (value - min) / (max - min);
        return outputMin + ratio * (outputMax - outputMin);
    }

    private static string EstimateAudioDuration(PoiDto poi)
    {
        var wordCount = poi.TtsScript
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Length;

        var seconds = Math.Max(70, wordCount / 3);
        var timeSpan = TimeSpan.FromSeconds(seconds);
        return $"{(int)timeSpan.TotalMinutes}:{timeSpan.Seconds:00}";
    }

    private static string GetNarrationLabel(NarrationMode mode)
    {
        return mode switch
        {
            NarrationMode.RecordedOnly => "audio thu sẵn",
            NarrationMode.Both => "audio kết hợp",
            _ => "tts tự động"
        };
    }

    private static string GetDifficultyLabel(int stopCount, int estimatedMinutes)
    {
        if (estimatedMinutes <= 30 || stopCount <= 3)
        {
            return "Nhanh";
        }

        if (estimatedMinutes <= 45 || stopCount <= 5)
        {
            return "Dễ đi bộ";
        }

        return "Khám phá";
    }
}
