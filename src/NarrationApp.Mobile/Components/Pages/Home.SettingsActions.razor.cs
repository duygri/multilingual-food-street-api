using Microsoft.Maui.Storage;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task SelectSettingsLanguageAsync(string languageCode)
    {
        await ApplySettingsStateChangeAsync(
            () =>
            {
                _state.ChangeAppLanguage(languageCode);
                Preferences.Default.Set(PreferredAppLanguageCodeKey, _state.SelectedAppLanguageCode);
                _cachePreloadStatusLabel = UiText.ReadyPreloadStatus();
                return Task.CompletedTask;
            });
    }
}
