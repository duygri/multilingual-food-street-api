namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task TryLoadContentBestEffortAsync(bool requestLocationPermission = false, bool preferNearbyPois = false)
    {
        try
        {
            await LoadContentAsync(
                requestLocationPermission: requestLocationPermission,
                preferNearbyPois: preferNearbyPois);
        }
        catch (Exception)
        {
            // Best-effort – content may fail but we still let the user proceed.
        }
    }

    private async Task CompletePermissionFlowAsync(bool permissionGranted)
    {
        await ApplyBackgroundTrackingStateChangeAsync(() => _state.CompletePermissions(permissionGranted));
    }
}
