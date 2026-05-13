using NarrationApp.Shared.Enums;

namespace NarrationApp.Mobile.Features.Home;

public enum VisitorIntroStep
{
    Welcome,
    Language,
    Permissions,
    Ready
}

public enum VisitorTab
{
    Map,
    Discover,
    Tours,
    Settings
}

public sealed record VisitorLanguageOption(string Code, string Label, string SubLabel, string ChipLabel);

public sealed record VisitorCategory(string Id, string Label, string MarkerLabel, string ToneKey = "is-history");

public sealed record VisitorPoi(
    string Id,
    string Name,
    string CategoryId,
    string CategoryLabel,
    string District,
    string StoryTag,
    string Description,
    string Highlight,
    double MapTopPercent,
    double MapLeftPercent,
    int DistanceMeters,
    string AudioDuration,
    string StatusLabel,
    double Latitude,
    double Longitude,
    int Priority = 1,
    int AvailableLanguageCount = 1,
    int GeofenceRadiusMeters = 30,
    string? ImageUrl = null,
    IReadOnlyList<string>? ReadyAudioLanguageCodesRaw = null,
    IReadOnlyList<VisitorPoiTranslation>? TranslationsRaw = null)
{
    public IReadOnlyList<string> ReadyAudioLanguageCodes { get; init; } = ReadyAudioLanguageCodesRaw ?? Array.Empty<string>();

    public IReadOnlyList<VisitorPoiTranslation> Translations { get; init; } = TranslationsRaw ?? Array.Empty<VisitorPoiTranslation>();

    public string GetNameForLanguage(string languageCode)
    {
        return ResolveTranslatedValue(languageCode, translation => translation.Title, Name);
    }

    public string GetDescriptionForLanguage(string languageCode)
    {
        return ResolveTranslatedValue(languageCode, translation => translation.Description, Description);
    }

    public string GetHighlightForLanguage(string languageCode)
    {
        return ResolveTranslatedValue(languageCode, translation => translation.Highlight, Highlight);
    }

    public string GetStoryForLanguage(string languageCode)
    {
        return ResolveTranslatedValue(languageCode, translation => translation.Story, Description);
    }

    private string ResolveTranslatedValue(
        string languageCode,
        Func<VisitorPoiTranslation, string> selector,
        string fallback)
    {
        var normalizedLanguageCode = NormalizeLanguageCode(languageCode);
        var value = Translations
            .Where(translation => string.Equals(
                NormalizeLanguageCode(translation.LanguageCode),
                normalizedLanguageCode,
                StringComparison.OrdinalIgnoreCase))
            .Select(selector)
            .FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));

        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string NormalizeLanguageCode(string languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "vi"
            : languageCode.Trim().ToLowerInvariant();
    }
}

public sealed record VisitorPoiTranslation(
    string LanguageCode,
    string Title,
    string Description,
    string Story,
    string Highlight);

public sealed record VisitorNotification(string Title, string Body, string TimeLabel, bool IsLive = false);

public sealed record VisitorTourCard(
    string Id,
    string Title,
    string StopCountLabel,
    string DurationLabel,
    string DifficultyLabel,
    string Description,
    IReadOnlyList<string> StopPoiIds);

public sealed record VisitorTourSession(
    string TourId,
    string TourTitle,
    int CurrentStopSequence,
    int TotalStops,
    string? NextPoiId,
    string NextPoiName,
    bool IsCompleted,
    bool IsServerBacked = false,
    TourSessionStatus? SyncStatus = null);
