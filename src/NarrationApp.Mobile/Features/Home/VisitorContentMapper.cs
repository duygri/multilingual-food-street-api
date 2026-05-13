using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Mobile.Features.Home;

public static partial class VisitorContentMapper
{
    public static VisitorContentSnapshot Map(
        IReadOnlyList<PoiDto> pois,
        IReadOnlyList<TourDto> tours,
        IReadOnlyList<CategoryDto> categories,
        VisitorLocationSnapshot? location = null,
        Uri? assetBaseAddress = null,
        IReadOnlyDictionary<int, IReadOnlyList<string>>? readyAudioLanguageCodesByPoiId = null,
        IReadOnlyList<ManagedLanguageDto>? languages = null)
    {
        var mappedCategories = MapCategories(categories, pois);
        var mappedPois = MapPois(pois, categories, location, assetBaseAddress, readyAudioLanguageCodesByPoiId);
        var mappedLanguages = MapLanguages(languages ?? []);
        var mappedTours = tours
            .Where(tour => tour.Status == TourStatus.Published)
            .Select(MapTour)
            .ToList();

        return new VisitorContentSnapshot(mappedPois, mappedTours, mappedCategories, mappedLanguages);
    }
}
