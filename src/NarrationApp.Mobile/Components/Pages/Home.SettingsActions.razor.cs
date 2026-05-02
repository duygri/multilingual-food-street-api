namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task SelectSettingsLanguageAsync(string languageCode)
    {
        await ApplySettingsStateChangeAsync(
            () => SelectAudioLanguageAsync(languageCode, keepPlayback: _state.IsAudioPlaying));
    }
}
