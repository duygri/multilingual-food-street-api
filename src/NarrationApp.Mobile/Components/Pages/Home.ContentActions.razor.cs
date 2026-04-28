namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task EnableLocationAsync()
    {
        await LoadContentAsync(requestLocationPermission: true, preferNearbyPois: true);
        _state.CompletePermissions(_state.LocationPermissionGranted);
    }

    private async Task SkipLocationAsync()
    {
        await LoadContentAsync();
        _state.CompletePermissions(false);
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
