using Microsoft.JSInterop;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task CenterMapOnUserAsync()
    {
        await JS.InvokeVoidAsync("visitorMap.centerOnUser", "discover-map");
    }
}
