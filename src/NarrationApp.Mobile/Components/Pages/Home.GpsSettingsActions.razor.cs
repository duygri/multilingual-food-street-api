using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private Task SetGpsBackgroundTrackingEnabledAsync(bool isEnabled)
    {
        _state.SetGpsBackgroundTrackingEnabled(isEnabled);
        return Task.CompletedTask;
    }

    private Task SetGpsAutoFocusEnabledAsync(bool isEnabled)
    {
        _state.SetGpsAutoFocusEnabled(isEnabled);
        return Task.CompletedTask;
    }

    private Task SetGpsAccuracyModeAsync(VisitorGpsAccuracyMode mode)
    {
        _state.SetGpsAccuracyMode(mode);
        return Task.CompletedTask;
    }
}
