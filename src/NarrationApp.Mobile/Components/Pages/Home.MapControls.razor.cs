using Microsoft.JSInterop;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task ZoomMapInAsync()
    {
        await JS.InvokeVoidAsync("visitorMap.zoomIn", "discover-map");
    }

    private async Task ZoomMapOutAsync()
    {
        await JS.InvokeVoidAsync("visitorMap.zoomOut", "discover-map");
    }
}
