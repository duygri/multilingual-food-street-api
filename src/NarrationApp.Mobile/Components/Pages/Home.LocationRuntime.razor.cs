using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void StartForegroundLocationLoopIfNeeded()
    {
        if (_foregroundLocationLoopTask is not null)
        {
            return;
        }

        _foregroundLocationLoopCts = new CancellationTokenSource();
        _foregroundLocationLoopTask = RunForegroundLocationLoopAsync(_foregroundLocationLoopCts.Token);
    }

    private async Task RunForegroundLocationLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(GetForegroundLocationRefreshInterval(), cancellationToken);
                await RefreshForegroundLocationAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Page is being disposed.
        }
    }

    private TimeSpan GetForegroundLocationRefreshInterval() =>
        _state.GpsPreferences.AccuracyMode switch
        {
            VisitorGpsAccuracyMode.High => TimeSpan.FromSeconds(6),
            VisitorGpsAccuracyMode.BatterySaver => TimeSpan.FromSeconds(20),
            _ => TimeSpan.FromSeconds(12)
        };

    private async Task RefreshForegroundLocationAsync(CancellationToken cancellationToken)
    {
        if (_state.CurrentStep != VisitorIntroStep.Ready || !_state.LocationPermissionGranted)
        {
            return;
        }

        var previousProximity = _state.ActiveProximity;
        var location = await LocationService.GetCurrentAsync(requestPermission: false, cancellationToken);

        _state.UpdateLocation(location);

        var nextProximity = ResolveNextProximity(location);
        await ApplyProximityNarrationAsync(previousProximity, nextProximity);

        await InvokeAsync(StateHasChanged);
    }
}
