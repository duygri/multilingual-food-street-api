using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private static readonly TimeSpan PresenceHeartbeatInterval = TimeSpan.FromSeconds(20);
    private CancellationTokenSource? _presenceHeartbeatLoopCts;
    private Task? _presenceHeartbeatLoopTask;

    private void StartPresenceHeartbeatLoopIfNeeded()
    {
        if (_presenceHeartbeatLoopTask is not null)
        {
            return;
        }

        _presenceHeartbeatLoopCts = new CancellationTokenSource();
        _presenceHeartbeatLoopTask = RunPresenceHeartbeatLoopAsync(_presenceHeartbeatLoopCts.Token);
    }

    private async Task RunPresenceHeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SendPresenceHeartbeatAsync(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(PresenceHeartbeatInterval, cancellationToken);
                await SendPresenceHeartbeatAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Page is being disposed.
        }
    }

    private async Task SendPresenceHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            await PresenceReporter.TrackAsync(cancellationToken);
            VisitorMobileDiagnostics.Log("Presence", "Heartbeat sent.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            VisitorMobileDiagnostics.Log("Presence", $"Heartbeat failed: {ex.Message}");
        }
    }
}
