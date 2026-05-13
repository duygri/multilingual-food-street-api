using Microsoft.Maui.Storage;

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
        if (permissionGranted)
        {
            Preferences.Default.Set(OnboardingWelcomeSeenKey, true);
            Preferences.Default.Set(OnboardingLanguageSelectedKey, true);
            Preferences.Default.Set(OnboardingLocationGrantedKey, true);
            Preferences.Default.Set(PreferredLanguageCodeKey, _state.SelectedLanguageCode);
        }

        await ApplyBackgroundTrackingStateChangeAsync(() => _state.CompletePermissions(permissionGranted));
    }
}
