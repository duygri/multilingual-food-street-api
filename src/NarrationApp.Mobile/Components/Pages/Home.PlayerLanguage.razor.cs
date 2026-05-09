using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task SelectFullPlayerLanguageAsync(string languageCode)
    {
        if (_state.SelectedPoi is null)
        {
            return;
        }

        _showFullPlayerLanguagePicker = false;
        await SelectAudioLanguageAsync(languageCode, keepPlayback: true);
    }

    private async Task SelectAudioLanguageAsync(string languageCode, bool keepPlayback)
    {
        if (!VisitorAudioLanguageSelector.CanUseForPoi(_state.SelectedPoi, languageCode))
        {
            return;
        }

        var shouldResume = keepPlayback && _state.IsAudioPlaying;
        _state.ChangeLanguage(languageCode);

        if (_state.SelectedPoi is not null)
        {
            await PrepareSelectedPoiAudioAsync(autoPlay: shouldResume, forceAutoPlay: shouldResume);
        }
    }

    private IReadOnlyList<VisitorLanguageOption> GetSelectedPoiAudioLanguages() =>
        VisitorAudioLanguageSelector.BuildForPoi(_state.SelectedPoi, _state.Languages);
}
