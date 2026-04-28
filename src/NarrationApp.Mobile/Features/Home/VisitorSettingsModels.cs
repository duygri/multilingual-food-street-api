namespace NarrationApp.Mobile.Features.Home;

public enum VisitorSettingsScreen
{
    Overview,
    Audio,
    Gps,
    Cache,
    History,
    About,
    Profile
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

public sealed record VisitorAboutLinkItem(
    string Label,
    string Description);
