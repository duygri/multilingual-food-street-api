namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private Task SelectSetupLanguage(string languageCode)
    {
        _state.SelectLanguage(languageCode);
        return Task.CompletedTask;
    }

    private Task ContinueFromLanguageSelection()
    {
        _state.AdvanceFromLanguageSelection();
        return Task.CompletedTask;
    }

    private async Task EnableLocationAsync()
    {
        await TryLoadContentBestEffortAsync(requestLocationPermission: true, preferNearbyPois: true);
        await CompletePermissionFlowAsync(_state.LocationPermissionGranted);
    }

    private async Task SkipLocationAsync()
    {
        await TryLoadContentBestEffortAsync();
        await CompletePermissionFlowAsync(false);
    }

    private async Task RefreshDiscoverAsync()
    {
        await LoadContentAsync(preferNearbyPois: _state.LocationPermissionGranted);
    }

    private void CycleLanguage()
    {
        var currentIndex = _state.Languages
            .Select((language, index) => new { language.Code, index })
            .First(item => item.Code == _state.SelectedLanguageCode)
            .index;

        var nextIndex = (currentIndex + 1) % _state.Languages.Count;
        _state.ChangeLanguage(_state.Languages[nextIndex].Code);
    }
}
