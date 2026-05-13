using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task RenderMapIfNeededAsync()
    {
        if (_state.CurrentStep != VisitorIntroStep.Ready || _state.CurrentTab != VisitorTab.Map)
        {
            await DisposeMapSurfaceAsync();
            _mapRenderState.Reset();
            return;
        }

        var mapSnapshot = VisitorMapSnapshotBuilder.Build(
            _state.FilteredPois,
            _state.SelectedPoiId,
            _state.CurrentLocation,
            GetCurrentWalkingRoute(),
            UiText.Pick("Your location", "Vị trí của bạn"));
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
            _isMapSurfaceMounted = true;
        }
        catch (JSException ex)
        {
            _mapRenderState.Reset();
            VisitorMobileDiagnostics.Log("MapRuntime", $"MapBox render failed: {ex.Message}");
        }
    }

    private async Task DisposeMapSurfaceAsync()
    {
        if (!_isMapSurfaceMounted)
        {
            return;
        }

        try
        {
            await JS.InvokeVoidAsync("visitorMap.dispose", "discover-map");
        }
        catch (JSException ex)
        {
            VisitorMobileDiagnostics.Log("MapRuntime", $"MapBox dispose failed: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            VisitorMobileDiagnostics.Log("MapRuntime", $"MapBox dispose skipped: {ex.Message}");
        }
        finally
        {
            _isMapSurfaceMounted = false;
        }
    }
}
