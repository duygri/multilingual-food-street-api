using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    public async ValueTask DisposeAsync()
    {
        VisitorPendingDeepLinkStore.PendingChanged -= HandlePendingDeepLinkChanged;
        await SendPresenceOfflineBestEffortAsync();

        _foregroundLocationLoopCts?.Cancel();
        _presenceHeartbeatLoopCts?.Cancel();

        await AwaitCanceledLoopAsync(_foregroundLocationLoopTask);
        await AwaitCanceledLoopAsync(_presenceHeartbeatLoopTask);
        await DisposeJsResourceAsync("visitorMap.dispose", "discover-map");
        await DisposeJsResourceAsync("visitorMap.dispose", "city-lens-map");
        await DisposeJsResourceAsync("visitorAudio.dispose");

        _mapBridge?.Dispose();
        _audioBridge?.Dispose();
        _foregroundLocationLoopCts?.Dispose();
        _presenceHeartbeatLoopCts?.Dispose();
    }
}
