using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Mobile.Features.Home;

public static partial class VisitorContentMapper
{
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
                var translations = MapTranslations(poi.Translations);
                var readyAudioLanguageCodes = ResolveReadyAudioLanguageCodes(poi.Id, readyAudioLanguageCodesByPoiId);
                var geofenceRadiusMeters = Math.Max(30, poi.Geofences.FirstOrDefault()?.RadiusMeters ?? 30);

                return new VisitorPoi(
                    Id: $"poi-{poi.Id}",
                    Name: poi.Name,
                    CategoryId: ResolveCategoryId(poi, categoriesById),
                    CategoryLabel: BuildCategoryLabel(poi, categoriesById),
                    District: "TP.HCM",
                    StoryTag: BuildStoryTag(poi, readyAudioLanguageCodes),
                    Description: poi.Description,
                    Highlight: ResolveDefaultHighlight(poi),
                    MapTopPercent: Normalize(poi.Lat, minLat, maxLat, 18d, 74d, index),
                    MapLeftPercent: Normalize(poi.Lng, minLng, maxLng, 18d, 72d, index),
                    DistanceMeters: 0,
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
                    ReadyAudioLanguageCodesRaw: readyAudioLanguageCodes,
                    TranslationsRaw: translations);
            })
            .ToList();

        return VisitorPoiDistanceProjector.Apply(mappedPois, location);
    }

    private static IReadOnlyList<VisitorPoiTranslation> MapTranslations(IReadOnlyList<TranslationDto> translations)
    {
        return translations
            .Where(translation => !string.IsNullOrWhiteSpace(translation.LanguageCode))
            .GroupBy(translation => translation.LanguageCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(translation => new VisitorPoiTranslation(
                NormalizeLanguageCode(translation.LanguageCode),
                translation.Title.Trim(),
                translation.Description.Trim(),
                translation.Story.Trim(),
                translation.Highlight.Trim()))
            .ToArray();
    }

    private static string ResolveDefaultHighlight(PoiDto poi)
    {
        var vietnameseHighlight = poi.Translations
            .FirstOrDefault(translation => string.Equals(
                NormalizeLanguageCode(translation.LanguageCode),
                "vi",
                StringComparison.OrdinalIgnoreCase))
            ?.Highlight;

        if (!string.IsNullOrWhiteSpace(vietnameseHighlight))
        {
            return vietnameseHighlight.Trim();
        }

        return !string.IsNullOrWhiteSpace(poi.TtsScript)
            ? poi.TtsScript.Trim()
            : "Audio guide sẵn sàng";
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

    private static string NormalizeLanguageCode(string languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "vi"
            : languageCode.Trim().ToLowerInvariant();
    }

    private static string BuildStoryTag(PoiDto poi, IReadOnlyList<string> readyAudioLanguageCodes)
    {
        if (readyAudioLanguageCodes.Count > 0)
        {
            return $"Live API • {readyAudioLanguageCodes.Count} ngôn ngữ thuyết minh";
        }

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
}
