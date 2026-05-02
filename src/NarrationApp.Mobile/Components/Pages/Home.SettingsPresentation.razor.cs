using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private VisitorModeSummary GetVisitorModeSummary() =>
        VisitorSettingsPresentationFormatter.CreateVisitorModeSummary();

    private VisitorSettingsOverviewSummary GetSettingsOverviewSummary() =>
        VisitorSettingsPresentationFormatter.CreateOverviewSummary(
            currentLanguageLabel: _state.CurrentLanguage.Label,
            audioSummary: GetAudioSettingsSummary(),
            gpsSummary: GetGpsSettingsSummary(),
            cacheSummary: GetAudioCacheSummary(),
            historySummary: GetListeningHistoryHeadline(),
            aboutSummary: $"{GetAboutVersionLabel()} • {GetAboutRuntimeLabel()}");

    private string GetAudioSettingsSummary() =>
        VisitorSettingsPresentationFormatter.FormatAudioSettingsSummary(
            _state.AudioPreferences.AutoPlayEnabled,
            _state.AudioPreferences.SourcePreference,
            _state.AudioPreferences.DefaultPlaybackSpeed);

    private string GetGpsSettingsSummary() =>
        VisitorSettingsPresentationFormatter.FormatGpsSettingsSummary(
            _state.LocationPermissionGranted,
            _state.GpsPreferences.AccuracyMode);
}
