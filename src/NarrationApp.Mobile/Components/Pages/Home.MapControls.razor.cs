using Microsoft.JSInterop;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task CenterMapOnUserAsync()
    {
        var location = await LocationService.GetCurrentAsync(requestPermission: true);
        _state.UpdateLocation(location);
        _mapRenderState.Reset();
        await RenderMapIfNeededAsync();
        await JS.InvokeVoidAsync("visitorMap.centerOnUser", "discover-map");
    }
}
