using System.Globalization;
using System.Text;

namespace NarrationApp.Mobile.Features.Home;

public static class VisitorPoiFilter
{
    public static IReadOnlyList<VisitorPoi> Apply(
        IReadOnlyList<VisitorPoi> pois,
        string selectedCategoryId,
        string searchTerm)
    {
        return pois
            .Where(poi => selectedCategoryId == "all" || poi.CategoryId == selectedCategoryId)
            .Where(poi => MatchesSearch(poi, searchTerm))
            .ToArray();
    }

    public static IReadOnlyList<VisitorPoi> WithReadyAudioForLanguage(
        IReadOnlyList<VisitorPoi> pois,
        string selectedLanguageCode)
    {
        return pois
            .Where(poi => HasReadyAudioForLanguage(poi, selectedLanguageCode))
            .ToArray();
    }

    public static bool HasReadyAudioForLanguage(VisitorPoi poi, string selectedLanguageCode)
    {
        return poi.ReadyAudioLanguageCodes.Any(languageCode =>
            string.Equals(languageCode, selectedLanguageCode, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesSearch(VisitorPoi poi, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return true;
        }

        var normalizedSearchTerm = Normalize(searchTerm);
        return Normalize(poi.Name).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase)
            || Normalize(poi.StoryTag).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase)
            || Normalize(poi.District).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
