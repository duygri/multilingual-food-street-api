using System.Net.Http.Json;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Mobile.Features.Home;

public interface IVisitorContentService
{
    Task<VisitorContentResult> LoadAsync(VisitorContentLoadRequest? request = null, CancellationToken cancellationToken = default);
}

public sealed class VisitorContentService(
    HttpClient httpClient,
    IVisitorLocationService locationService,
    IVisitorOfflineCacheStore offlineCacheStore) : IVisitorContentService
{
    public async Task<VisitorContentResult> LoadAsync(VisitorContentLoadRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new VisitorContentLoadRequest();
        var location = await GetLocationSnapshotAsync(request, cancellationToken);

        try
        {
            var poisEndpoint = BuildPoisEndpoint(request, location);
            var poisTask = httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<PoiDto>>>(poisEndpoint, cancellationToken);
            var toursTask = httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<TourDto>>>("api/tours", cancellationToken);
            var categoriesTask = httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<CategoryDto>>>("api/categories", cancellationToken);

            await Task.WhenAll(poisTask, toursTask, categoriesTask);

            var poisResponse = await poisTask;
            var toursResponse = await toursTask;
            var categoriesResponse = await categoriesTask;

            var pois = await ResolvePoisAsync(request, location, poisEndpoint, poisResponse, cancellationToken);
            var tours = toursResponse?.Data ?? [];
            var categories = categoriesResponse?.Data ?? [];
            var readyAudioLanguageCodesByPoiId = await LoadReadyAudioLanguageCodesByPoiAsync(pois, cancellationToken);

            var snapshot = VisitorContentMapper.Map(
                pois,
                tours,
                categories,
                location,
                httpClient.BaseAddress,
                readyAudioLanguageCodesByPoiId);
            await SaveContentSnapshotAsync(snapshot, cancellationToken);
            return new VisitorContentResult(
                snapshot,
                IsFallback: false,
                SourceLabel: "Live API",
                Message: BuildSuccessMessage(snapshot, location, usedNearbyFallback: ShouldUseNearbyFallback(request, location, poisEndpoint, poisResponse)),
                Location: location);
        }
        catch (Exception ex)
        {
            var cachedSnapshot = await LoadContentSnapshotAsync(cancellationToken);
            if (cachedSnapshot is not null)
            {
                return new VisitorContentResult(
                    cachedSnapshot,
                    IsFallback: true,
                    SourceLabel: "Offline cache",
                    Message: $"Đang dùng dữ liệu offline đã lưu trên máy. Không chạm được API thật. {ex.Message}",
                    Location: location);
            }

            return new VisitorContentResult(
                new VisitorContentSnapshot([], []),
                IsFallback: true,
                SourceLabel: "API unavailable",
                Message: $"Không chạm được API thật. {ex.Message}",
                Location: location);
        }
    }

    private async Task SaveContentSnapshotAsync(VisitorContentSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            await offlineCacheStore.SaveContentSnapshotAsync(snapshot, cancellationToken);
        }
        catch
        {
            // Cache failures should never block live content.
        }
    }

    private async Task<VisitorContentSnapshot?> LoadContentSnapshotAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await offlineCacheStore.LoadContentSnapshotAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task<VisitorLocationSnapshot> GetLocationSnapshotAsync(VisitorContentLoadRequest request, CancellationToken cancellationToken)
    {
        if (!request.PreferNearbyPois && !request.RequestLocationPermission)
        {
            return VisitorLocationSnapshot.Disabled();
        }

        try
        {
            return await locationService.GetCurrentAsync(request.RequestLocationPermission, cancellationToken);
        }
        catch (Exception ex)
        {
            return VisitorLocationSnapshot.Disabled($"Không lấy được vị trí: {ex.Message}");
        }
    }

    private static string BuildPoisEndpoint(VisitorContentLoadRequest request, VisitorLocationSnapshot location)
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

    private async Task<IReadOnlyList<PoiDto>> ResolvePoisAsync(
        VisitorContentLoadRequest request,
        VisitorLocationSnapshot location,
        string poisEndpoint,
        ApiResponse<IReadOnlyList<PoiDto>>? poisResponse,
        CancellationToken cancellationToken)
    {
        var pois = poisResponse?.Data ?? [];
        if (!ShouldUseNearbyFallback(request, location, poisEndpoint, poisResponse))
        {
            return pois;
        }

        var fallbackResponse = await httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<PoiDto>>>("api/pois", cancellationToken);
        return fallbackResponse?.Data ?? [];
    }

    private static bool ShouldUseNearbyFallback(
        VisitorContentLoadRequest request,
        VisitorLocationSnapshot location,
        string poisEndpoint,
        ApiResponse<IReadOnlyList<PoiDto>>? poisResponse)
    {
        return request.PreferNearbyPois
            && location.PermissionGranted
            && location.IsLocationAvailable
            && location.Latitude is not null
            && location.Longitude is not null
            && string.Equals(poisEndpoint, BuildPoisEndpoint(request, location), StringComparison.Ordinal)
            && poisEndpoint.StartsWith("api/pois/near", StringComparison.OrdinalIgnoreCase)
            && (poisResponse?.Data?.Count ?? 0) == 0;
    }

    private static string BuildSuccessMessage(
        VisitorContentSnapshot snapshot,
        VisitorLocationSnapshot location,
        bool usedNearbyFallback)
    {
        if (usedNearbyFallback)
        {
            return $"Không có POI gần vị trí hiện tại. Đang hiển thị toàn bộ {snapshot.Pois.Count} POI từ máy chủ.";
        }

        if (location.PermissionGranted && location.IsLocationAvailable)
        {
            return $"Đã tải {snapshot.Pois.Count} POI gần bạn và {snapshot.Tours.Count} tour từ máy chủ.";
        }

        return $"Đã tải {snapshot.Pois.Count} POI và {snapshot.Tours.Count} tour từ máy chủ.";
    }

    private async Task<IReadOnlyDictionary<int, IReadOnlyList<string>>> LoadReadyAudioLanguageCodesByPoiAsync(
        IReadOnlyList<PoiDto> pois,
        CancellationToken cancellationToken)
    {
        var publishedPoiIds = pois
            .Where(poi => poi.Status == PoiStatus.Published || poi.Status == PoiStatus.Updated)
            .Select(poi => poi.Id)
            .Distinct()
            .ToArray();

        if (publishedPoiIds.Length == 0)
        {
            return new Dictionary<int, IReadOnlyList<string>>();
        }

        var tasksByPoiId = publishedPoiIds.ToDictionary(
            poiId => poiId,
            poiId => LoadReadyAudioLanguageCodesAsync(poiId, cancellationToken));

        await Task.WhenAll(tasksByPoiId.Values);

        return tasksByPoiId.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Result);
    }

    private async Task<IReadOnlyList<string>> LoadReadyAudioLanguageCodesAsync(int poiId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<AudioDto>>>(
                $"api/audio?poiId={poiId}",
                cancellationToken);

            return (response?.Data ?? [])
                .Where(asset => asset.Status == AudioStatus.Ready)
                .Select(asset => asset.LanguageCode?.Trim())
                .Where(languageCode => !string.IsNullOrWhiteSpace(languageCode))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(languageCode => languageCode, StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToArray();
        }
        catch
        {
            return [];
        }
    }
}

public static class VisitorContentMapper
{
    public static VisitorContentSnapshot Map(
        IReadOnlyList<PoiDto> pois,
        IReadOnlyList<TourDto> tours,
        IReadOnlyList<CategoryDto> categories,
        VisitorLocationSnapshot? location = null,
        Uri? assetBaseAddress = null,
        IReadOnlyDictionary<int, IReadOnlyList<string>>? readyAudioLanguageCodesByPoiId = null)
    {
        var mappedCategories = MapCategories(categories, pois);
        var mappedPois = MapPois(pois, categories, location, assetBaseAddress, readyAudioLanguageCodesByPoiId);
        var mappedTours = tours
            .Where(tour => tour.Status == TourStatus.Published)
            .Select(MapTour)
            .ToList();

        return new VisitorContentSnapshot(mappedPois, mappedTours, mappedCategories);
    }

    private static IReadOnlyList<VisitorCategory> MapCategories(IReadOnlyList<CategoryDto> categories, IReadOnlyList<PoiDto> pois)
    {
        if (categories.Count > 0)
        {
            return categories
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(category => new VisitorCategory(
                    category.Slug,
                    category.Name,
                    ResolveCategoryIcon(category),
                    VisitorCategoryPresentationFormatter.GetCategoryTone(category.Slug, [], category.Name)))
                .ToList();
        }

        return pois
            .Where(poi => !string.IsNullOrWhiteSpace(poi.CategoryName))
            .GroupBy(poi => ResolveFallbackCategoryId(poi), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var firstPoi = group.First();
                return VisitorCategoryPresentationFormatter.CreateCategory(
                    group.Key,
                    firstPoi.CategoryName ?? firstPoi.Name);
            })
            .OrderBy(category => category.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<VisitorPoi> MapPois(
        IReadOnlyList<PoiDto> pois,
        IReadOnlyList<CategoryDto> categories,
        VisitorLocationSnapshot? location,
        Uri? assetBaseAddress,
        IReadOnlyDictionary<int, IReadOnlyList<string>>? readyAudioLanguageCodesByPoiId)
    {
        var categoriesById = categories
            .GroupBy(category => category.Id)
            .Select(group => group.First())
            .ToDictionary(category => category.Id, category => category);

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

        var mappedPois = publishedPois
            .Select((poi, index) =>
            {
                var translationCount = poi.Translations
                    .Select(translation => translation.LanguageCode)
                    .Where(languageCode => !string.IsNullOrWhiteSpace(languageCode))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
                var readyAudioLanguageCodes = ResolveReadyAudioLanguageCodes(poi.Id, readyAudioLanguageCodesByPoiId);
                var geofenceRadiusMeters = Math.Max(30, poi.Geofences.FirstOrDefault()?.RadiusMeters ?? 30);
                var nearbyDistanceMeters = Math.Max(geofenceRadiusMeters + 75, 120 + index * 30);

                return new VisitorPoi(
                    Id: $"poi-{poi.Id}",
                    Name: poi.Name,
                    CategoryId: ResolveCategoryId(poi, categoriesById),
                    CategoryLabel: BuildCategoryLabel(poi, categoriesById),
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
                    AvailableLanguageCount: readyAudioLanguageCodes.Count > 0
                        ? readyAudioLanguageCodes.Count
                        : Math.Max(1, translationCount + 1),
                    GeofenceRadiusMeters: geofenceRadiusMeters,
                    ImageUrl: ResolveImageUrl(poi.ImageUrl, assetBaseAddress),
                    ReadyAudioLanguageCodesRaw: readyAudioLanguageCodes);
            })
            .ToList();

        return VisitorPoiDistanceProjector.Apply(mappedPois, location);
    }

    private static string? ResolveImageUrl(string? imageUrl, Uri? assetBaseAddress)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        if (assetBaseAddress is not null && Uri.TryCreate(assetBaseAddress, imageUrl, out var combinedUri))
        {
            return combinedUri.ToString();
        }

        return imageUrl;
    }

    private static IReadOnlyList<string> ResolveReadyAudioLanguageCodes(
        int poiId,
        IReadOnlyDictionary<int, IReadOnlyList<string>>? readyAudioLanguageCodesByPoiId)
    {
        if (readyAudioLanguageCodesByPoiId is null
            || !readyAudioLanguageCodesByPoiId.TryGetValue(poiId, out var readyAudioLanguageCodes))
        {
            return [];
        }

        return readyAudioLanguageCodes
            .Where(languageCode => !string.IsNullOrWhiteSpace(languageCode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(languageCode => languageCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static VisitorTourCard MapTour(TourDto tour)
    {
        return new VisitorTourCard(
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

    private static string BuildCategoryLabel(PoiDto poi, IReadOnlyDictionary<int, CategoryDto> categoriesById)
    {
        if (poi.CategoryId is int categoryId && categoriesById.TryGetValue(categoryId, out var category))
        {
            return category.Name;
        }

        if (!string.IsNullOrWhiteSpace(poi.CategoryName))
        {
            return poi.CategoryName.Trim();
        }

        return ResolveFallbackCategoryId(poi) switch
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

    private static string ResolveCategoryId(PoiDto poi, IReadOnlyDictionary<int, CategoryDto> categoriesById)
    {
        if (poi.CategoryId is int categoryId && categoriesById.TryGetValue(categoryId, out var category))
        {
            return category.Slug;
        }

        return ResolveFallbackCategoryId(poi);
    }

    private static string ResolveFallbackCategoryId(PoiDto poi)
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

    private static string ResolveCategoryIcon(CategoryDto category)
    {
        var icon = category.Icon?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(icon) && icon.Length <= 3)
        {
            return icon;
        }

        return VisitorCategoryPresentationFormatter.GetCategoryIcon(
            category.Slug,
            [],
            $"{category.Name} {icon}".Trim());
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
