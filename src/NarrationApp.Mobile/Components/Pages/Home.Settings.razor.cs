using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NarrationApp.Mobile.Components.Pages.Sections;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void OpenSettingsScreen(VisitorSettingsScreen screen)
    {
        SyncProfileDraftFromSession();
        _profileStatusMessage = null;
        _profileErrorMessage = null;
        _state.OpenSettingsScreen(screen);
    }

    private Task CloseSettingsScreen()
    {
        _profileStatusMessage = null;
        _profileErrorMessage = null;
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
            await LoadContentAsync();
        }
        else
        {
            await LoadContentAsync(requestLocationPermission: true, preferNearbyPois: true);
        }

        _state.CompletePermissions(_state.LocationPermissionGranted);
        _state.SwitchTab(VisitorTab.Settings);
    }

}
