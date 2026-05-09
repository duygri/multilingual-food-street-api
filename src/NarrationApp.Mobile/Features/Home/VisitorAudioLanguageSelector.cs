namespace NarrationApp.Mobile.Features.Home;

public static class VisitorAudioLanguageSelector
{
    public static IReadOnlyList<VisitorLanguageOption> BuildForPoi(
        VisitorPoi? poi,
        IReadOnlyList<VisitorLanguageOption> languages)
    {
        if (poi?.ReadyAudioLanguageCodes.Count > 0)
        {
            var languagesByCode = languages
                .GroupBy(language => language.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            return poi.ReadyAudioLanguageCodes
                .Where(languageCode => !string.IsNullOrWhiteSpace(languageCode))
                .Select(languageCode => languageCode.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(languageCode => languagesByCode.TryGetValue(languageCode, out var language)
                    ? language
                    : CreateFallback(languageCode))
                .ToArray();
        }

        return languages;
    }

    public static bool CanUseForPoi(VisitorPoi? poi, string languageCode)
    {
        if (poi?.ReadyAudioLanguageCodes.Count > 0)
        {
            return poi.ReadyAudioLanguageCodes
                .Any(readyLanguageCode => string.Equals(readyLanguageCode, languageCode, StringComparison.OrdinalIgnoreCase));
        }

        return true;
    }

    private static VisitorLanguageOption CreateFallback(string languageCode) =>
        VisitorLanguageCatalog.CreateOption(languageCode);
}
