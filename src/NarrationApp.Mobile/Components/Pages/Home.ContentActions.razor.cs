using Microsoft.Maui.Storage;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private Task SelectSetupLanguage(string languageCode)
    {
        _state.ChangeAppLanguage(languageCode);
        _state.SelectLanguage(languageCode);
        Preferences.Default.Set(PreferredAppLanguageCodeKey, _state.SelectedAppLanguageCode);
        Preferences.Default.Set(PreferredLanguageCodeKey, languageCode);
        _cachePreloadStatusLabel = UiText.ReadyPreloadStatus();
        return Task.CompletedTask;
    }

    private Task ContinueFromLanguageSelection()
    {
        Preferences.Default.Set(OnboardingWelcomeSeenKey, true);
        Preferences.Default.Set(OnboardingLanguageSelectedKey, true);
        Preferences.Default.Set(PreferredAppLanguageCodeKey, _state.SelectedAppLanguageCode);
        Preferences.Default.Set(PreferredLanguageCodeKey, _state.SelectedLanguageCode);
        _state.AdvanceFromLanguageSelection();
        return Task.CompletedTask;
    }

    private Task ContinueFromWelcomeAsync()
    {
        Preferences.Default.Set(OnboardingWelcomeSeenKey, true);
        _state.ContinueFromWelcome();
        return Task.CompletedTask;
    }

    private async Task EnableLocationAsync()
    {
        await TryLoadContentBestEffortAsync(requestLocationPermission: true, preferNearbyPois: true);
        if (_state.LocationPermissionGranted)
        {
            Preferences.Default.Set(OnboardingWelcomeSeenKey, true);
            Preferences.Default.Set(OnboardingLanguageSelectedKey, true);
            Preferences.Default.Set(OnboardingLocationGrantedKey, true);
        }

        await CompletePermissionFlowAsync(_state.LocationPermissionGranted);
    }

    private async Task RefreshDiscoverAsync()
    {
        await LoadContentAsync(preferNearbyPois: _state.LocationPermissionGranted);
    }

    private Task CycleLanguage()
    {
        var appLanguages = _state.AppLanguages;
        var currentIndex = appLanguages
            .Select((language, index) => new { language.Code, index })
            .First(item => item.Code == _state.SelectedAppLanguageCode)
            .index;

        var nextLanguageCode = appLanguages[(currentIndex + 1) % appLanguages.Count].Code;
        _state.ChangeAppLanguage(nextLanguageCode);
        Preferences.Default.Set(PreferredAppLanguageCodeKey, nextLanguageCode);
        _cachePreloadStatusLabel = UiText.ReadyPreloadStatus();
        return Task.CompletedTask;
    }
}
