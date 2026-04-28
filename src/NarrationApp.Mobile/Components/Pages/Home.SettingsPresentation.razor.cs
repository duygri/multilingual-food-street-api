using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private string GetAccountModeDescription() =>
        VisitorSettingsPresentationFormatter.GetAccountModeDescription();

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
