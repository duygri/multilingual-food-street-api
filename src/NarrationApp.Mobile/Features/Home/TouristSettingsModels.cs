namespace NarrationApp.Mobile.Features.Home;

public enum TouristSettingsScreen
{
    Overview,
    Audio,
    Gps,
    Cache,
    History,
    About,
    Profile
}

public enum TouristAudioSourcePreference
{
    RecordedFirst,
    TextToSpeech
}

public enum TouristGpsAccuracyMode
{
    Adaptive,
    High,
    BatterySaver
}

public sealed record TouristAudioPreferences(
    bool AutoPlayEnabled,
    bool SpokenAnnouncementsEnabled,
    bool AutoAdvanceEnabled,
    TouristAudioSourcePreference SourcePreference,
    double DefaultPlaybackSpeed,
    string CooldownLabel,
    string QueueLabel);

public sealed record TouristGpsPreferences(
    bool BackgroundTrackingEnabled,
    bool AutoFocusEnabled,
    TouristGpsAccuracyMode AccuracyMode,
    int BatteryPercent,
    string StatusLabel,
    string BatteryLabel);

public sealed record TouristCachedAudioItem(
    string Id,
    string PoiId,
    string PoiName,
    string LanguageCode,
    string SourceLabel,
    double SizeMb,
    string UpdatedLabel);

public sealed record TouristListeningHistoryDay(
    string Label,
    IReadOnlyList<TouristListeningHistoryEntry> Entries);

public sealed record TouristListeningHistoryEntry(
    string Id,
    string PoiId,
    string PoiName,
    string CategoryLabel,
    string LanguageCode,
    string TimeLabel,
    string DurationLabel,
    int CompletionPercent);

public sealed record TouristSettingsStat(
    string Value,
    string Label);

public sealed record TouristAboutLinkItem(
    string Label,
    string Description);
