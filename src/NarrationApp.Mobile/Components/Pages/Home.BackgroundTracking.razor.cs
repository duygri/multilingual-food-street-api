using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task ApplyBackgroundTrackingStateChangeAsync(Action applyStateChange, CancellationToken cancellationToken = default)
    {
        applyStateChange();
        await SyncBackgroundTrackingAsync(cancellationToken);
    }

    private async Task SyncBackgroundTrackingAsync(CancellationToken cancellationToken = default)
    {
        var status = await BackgroundLocationRuntime.ApplyAsync(
            new VisitorBackgroundTrackingRequest(
                _state.GpsPreferences.BackgroundTrackingEnabled,
                _state.LocationPermissionGranted,
                _state.GpsPreferences.AccuracyMode),
            cancellationToken);

        _state.ApplyBackgroundTrackingStatus(status);
    }
}
