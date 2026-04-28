using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    public async ValueTask DisposeAsync()
    {
        VisitorPendingDeepLinkStore.PendingChanged -= HandlePendingDeepLinkChanged;

        try
        {
            await JS.InvokeVoidAsync("visitorMap.dispose", "discover-map");
        }
        catch
        {
            // Best-effort cleanup while the page is tearing down.
        }

        try
        {
            await JS.InvokeVoidAsync("visitorAudio.dispose");
        }
        catch
        {
            // Best-effort cleanup while the page is tearing down.
        }

        _mapBridge?.Dispose();
        _audioBridge?.Dispose();
    }
}
