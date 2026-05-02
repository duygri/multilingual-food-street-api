using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void OpenSettingsScreen(VisitorSettingsScreen screen)
    {
        ClearSettingsFeedback();
        _state.OpenSettingsScreen(screen);
    }

    private Task CloseSettingsScreen()
    {
        ClearSettingsFeedback();
        _state.CloseSettingsScreen();
        return Task.CompletedTask;
    }

    private async Task OpenPoiFromHistoryAsync(string poiId)
    {
        await CloseSettingsScreen();
        _state.SwitchTab(VisitorTab.Discover);
        await OpenPoiDetailAsync(poiId);
    }

    private async Task TogglePermissionAsync()
    {
        if (_state.LocationPermissionGranted)
        {
            await TryLoadContentBestEffortAsync();
        }
        else
        {
            await TryLoadContentBestEffortAsync(requestLocationPermission: true, preferNearbyPois: true);
        }

        await CompletePermissionFlowAsync(_state.LocationPermissionGranted);
        _state.SwitchTab(VisitorTab.Settings);
    }

}
