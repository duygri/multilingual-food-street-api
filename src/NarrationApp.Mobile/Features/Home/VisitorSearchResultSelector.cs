using System.Globalization;
using System.Text;

namespace NarrationApp.Mobile.Features.Home;

public static class VisitorSearchResultSelector
{
    public static IReadOnlyList<VisitorPoi> GetPoiResults(IReadOnlyList<VisitorPoi> filteredPois, string searchTerm) =>
        string.IsNullOrWhiteSpace(searchTerm)
            ? filteredPois
                .OrderBy(poi => poi.DistanceMeters)
                .Take(6)
                .ToList()
            : filteredPois
                .OrderBy(poi => poi.DistanceMeters)
                .Take(8)
                .ToList();

    public static IReadOnlyList<VisitorTourCard> GetTourResults(
        IReadOnlyList<VisitorTourCard> tours,
        IReadOnlyList<VisitorPoi> pois,
        string selectedCategoryId,
        string searchTerm)
    {
        IEnumerable<VisitorTourCard> filteredTours = tours;

        if (selectedCategoryId != "all")
        {
            filteredTours = filteredTours.Where(tour =>
                tour.StopPoiIds
                    .Select(stopId => pois.FirstOrDefault(poi => poi.Id == stopId))
                    .Any(poi => poi?.CategoryId == selectedCategoryId));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredTours = filteredTours.Where(tour => MatchesTourSearch(tour, pois, searchTerm));
        }

        return filteredTours.Take(4).ToList();
    }

    public static int GetResultCount(IReadOnlyList<VisitorPoi> poiResults, IReadOnlyList<VisitorTourCard> tourResults) =>
        poiResults.Count + tourResults.Count;

    public static bool MatchesTourSearch(VisitorTourCard tour, IReadOnlyList<VisitorPoi> pois, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return true;
        }

        var normalizedSearchTerm = NormalizeSearchValue(searchTerm);
        return NormalizeSearchValue(tour.Title).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase)
            || NormalizeSearchValue(tour.Description).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase)
            || tour.StopPoiIds
                .Select(stopId => pois.FirstOrDefault(poi => poi.Id == stopId)?.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Any(name => NormalizeSearchValue(name!).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase));
    }

    public static string NormalizeSearchValue(string value)
    {
        var foldedValue = value
            .Replace('đ', 'd')
            .Replace('Đ', 'D');

        var characters = foldedValue
            .Normalize(NormalizationForm.FormD)
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark);

        return new string(characters.ToArray()).Normalize(NormalizationForm.FormC);
    }
}
