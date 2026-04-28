namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task CycleFullPlayerLanguageAsync()
    {
        if (_state.SelectedPoi is null || _state.Languages.Count == 0)
        {
            return;
        }

        var currentIndex = _state.Languages
            .Select((language, index) => new { language.Code, index })
            .First(item => item.Code == _state.SelectedLanguageCode)
            .index;

        var nextIndex = (currentIndex + 1) % _state.Languages.Count;
        await SelectAudioLanguageAsync(_state.Languages[nextIndex].Code, keepPlayback: true);
    }

    private async Task SelectAudioLanguageAsync(string languageCode, bool keepPlayback)
    {
        var shouldResume = keepPlayback && _state.IsAudioPlaying;
        _state.ChangeLanguage(languageCode);

        if (_state.SelectedPoi is not null)
        {
            await PrepareSelectedPoiAudioAsync(autoPlay: shouldResume, forceAutoPlay: shouldResume);
        }
    }
}
