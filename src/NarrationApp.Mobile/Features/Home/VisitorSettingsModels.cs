namespace NarrationApp.Mobile.Features.Home;

public enum VisitorSettingsScreen
{
    Overview,
    Audio,
    Gps,
    Cache,
    History,
    About
}

public enum VisitorAudioSourcePreference
{
    RecordedFirst,
    TextToSpeech
}

public enum VisitorGpsAccuracyMode
{
    Adaptive,
    High,
    BatterySaver
}

public sealed record VisitorAudioPreferences(
    bool AutoPlayEnabled,
    bool SpokenAnnouncementsEnabled,
    bool AutoAdvanceEnabled,
    VisitorAudioSourcePreference SourcePreference,
    double DefaultPlaybackSpeed,
    string CooldownLabel,
    string QueueLabel);

public sealed record VisitorGpsPreferences(
    bool BackgroundTrackingEnabled,
    bool AutoFocusEnabled,
    VisitorGpsAccuracyMode AccuracyMode,
    int BatteryPercent,
    string StatusLabel,
    string BatteryLabel);

public sealed record VisitorDebugEvent(string TimeLabel, string Message);

public sealed record VisitorCachedAudioItem(
    string Id,
    string PoiId,
    string PoiName,
    string LanguageCode,
    string SourceLabel,
    double SizeMb,
    string UpdatedLabel);

public sealed record VisitorListeningHistoryDay(
    string Label,
    IReadOnlyList<VisitorListeningHistoryEntry> Entries);

public sealed record VisitorListeningHistoryEntry(
    string Id,
    string PoiId,
    string PoiName,
    string CategoryLabel,
    string LanguageCode,
    string TimeLabel,
    string DurationLabel,
    int CompletionPercent);

public sealed record VisitorSettingsStat(
    string Value,
    string Label);

public sealed record VisitorModeSummary(
    string Title,
    string Subtitle,
    string ModeLabel,
    string Initials);

public sealed record VisitorSettingsOverviewSummary(
    string CurrentLanguageLabel,
    string AudioSummary,
    string GpsSummary,
    string CacheSummary,
    string HistorySummary,
    string AboutSummary);

public sealed record VisitorSearchResultsSummary(
    int ResultCount,
    IReadOnlyList<VisitorPoi> PoiResults,
    IReadOnlyList<VisitorTourCard> TourResults);

public sealed record VisitorAboutLinkItem(
    string Label,
    string Description);
