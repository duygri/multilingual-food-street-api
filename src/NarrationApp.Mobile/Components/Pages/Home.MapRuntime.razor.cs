using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task RenderMapIfNeededAsync()
    {
        if (_state.CurrentStep != VisitorIntroStep.Ready || _state.CurrentTab != VisitorTab.Map)
        {
            _mapRenderState.Reset();
            return;
        }

        var mapSnapshot = VisitorMapSnapshotBuilder.Build(_state.FilteredPois, _state.SelectedPoiId, _state.CurrentLocation);
        if (!_mapRenderState.ShouldRender(mapSnapshot))
        {
            return;
        }

        await JS.InvokeVoidAsync(
            "visitorMap.render",
            "discover-map",
            MapOptions.AccessToken,
            MapOptions.StyleUrl,
            mapSnapshot,
            _mapBridge);
    }
}
