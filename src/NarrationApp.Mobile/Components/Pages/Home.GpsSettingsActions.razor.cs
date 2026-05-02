using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task SetGpsBackgroundTrackingEnabledAsync(bool isEnabled)
    {
        await ApplySettingsStateChangeAsync(
            () => ApplyBackgroundTrackingStateChangeAsync(() => _state.SetGpsBackgroundTrackingEnabled(isEnabled)));
    }

    private Task SetGpsAutoFocusEnabledAsync(bool isEnabled)
    {
        return ApplySettingsStateChangeAsync(() => _state.SetGpsAutoFocusEnabled(isEnabled));
    }

    private async Task SetGpsAccuracyModeAsync(VisitorGpsAccuracyMode mode)
    {
        await ApplySettingsStateChangeAsync(
            () => ApplyBackgroundTrackingStateChangeAsync(() => _state.SetGpsAccuracyMode(mode)));
    }
}
