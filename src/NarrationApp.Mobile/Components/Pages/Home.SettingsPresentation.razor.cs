using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private VisitorModeSummary GetVisitorModeSummary() =>
        UiText.CreateVisitorModeSummary();

    private VisitorSettingsOverviewSummary GetSettingsOverviewSummary() =>
        VisitorSettingsPresentationFormatter.CreateOverviewSummary(
            currentLanguageLabel: UiText.LocalizeLanguageLabel(_state.CurrentAppLanguage.Label),
            audioSummary: GetAudioSettingsSummary(),
            gpsSummary: GetGpsSettingsSummary(),
            cacheSummary: GetAudioCacheSummary(),
            historySummary: GetListeningHistoryHeadline(),
            aboutSummary: $"{GetAboutVersionLabel()} • {GetAboutRuntimeLabel()}");

    private string GetAudioSettingsSummary() =>
        UiText.FormatAudioSettingsSummary(
            _state.AudioPreferences.AutoPlayEnabled,
            _state.AudioPreferences.SourcePreference,
            _state.AudioPreferences.DefaultPlaybackSpeed);

    private string GetGpsSettingsSummary() =>
        UiText.FormatGpsSettingsSummary(
            _state.LocationPermissionGranted,
            _state.GpsPreferences.AccuracyMode);
}
