using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private static bool IsPlaceholderToken(string token)
        => string.IsNullOrWhiteSpace(token)
           || token.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);

    private async Task RenderMapIfNeededAsync()
    {
        if (_state.CurrentStep != VisitorIntroStep.Ready || _state.CurrentTab != VisitorTab.Map)
        {
            _mapRenderState.Reset();
            return;
        }

        if (IsPlaceholderToken(MapOptions.AccessToken))
        {
            return;
        }

        var mapSnapshot = VisitorMapSnapshotBuilder.Build(_state.FilteredPois, _state.SelectedPoiId, _state.CurrentLocation);
        if (!_mapRenderState.ShouldRender(mapSnapshot))
        {
            return;
        }

        try
        {
            await JS.InvokeVoidAsync(
                "visitorMap.render",
                "discover-map",
                MapOptions.AccessToken,
                MapOptions.StyleUrl,
                mapSnapshot,
                _mapBridge);
        }
        catch (JSException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapRuntime] MapBox render failed: {ex.Message}");
        }
    }
}
