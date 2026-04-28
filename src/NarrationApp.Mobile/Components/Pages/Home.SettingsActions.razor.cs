namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task SelectSettingsLanguageAsync(string languageCode)
    {
        _profileStatusMessage = null;
        _profileErrorMessage = null;
        await SelectAudioLanguageAsync(languageCode, keepPlayback: _state.IsAudioPlaying);
    }
}
