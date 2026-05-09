using System.Net.Http.Json;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Languages;
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
            var languagesTask = LoadManagedLanguagesAsync(cancellationToken);

            await Task.WhenAll(poisTask, toursTask, categoriesTask, languagesTask);

            var poisResponse = await poisTask;
            var toursResponse = await toursTask;
            var categoriesResponse = await categoriesTask;
            var languages = await languagesTask;

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
                readyAudioLanguageCodesByPoiId,
                languages);
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

    private async Task<IReadOnlyList<ManagedLanguageDto>> LoadManagedLanguagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<ManagedLanguageDto>>>(
                "api/languages",
                cancellationToken);

            return response?.Data ?? [];
        }
        catch
        {
            return [];
        }
    }
}
