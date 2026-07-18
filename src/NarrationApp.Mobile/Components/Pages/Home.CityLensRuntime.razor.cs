using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task RenderCityLensIfNeededAsync()
    {
        if (_state.CurrentStep != VisitorIntroStep.Ready
            || _state.CurrentTab != VisitorTab.Discover
            || IsDiscoverPoiDetailVisible)
        {
            await DisposeCityLensSurfaceAsync();
            _cityLensRenderState.Reset();
            return;
        }

        var mapSnapshot = VisitorMapSnapshotBuilder.Build(
            _state.DiscoverPois,
            _state.SelectedPoiId,
            _state.CurrentLocation,
            userLocationLabel: UiText.Pick("Your location", "Vị trí của bạn"));
        if (!_cityLensRenderState.ShouldRender(mapSnapshot))
        {
            return;
        }

        try
        {
            await JS.InvokeVoidAsync(
                "visitorMap.render",
                "city-lens-map",
                MapOptions.AccessToken,
                MapOptions.StyleUrl,
                mapSnapshot,
                _mapBridge,
                new { interactive = false, markersInteractive = false, showRadius = false });
            _isCityLensSurfaceMounted = true;
        }
        catch (JSException ex)
        {
            _cityLensRenderState.Reset();
            VisitorMobileDiagnostics.Log("CityLensRuntime", $"City Lens render failed: {ex.Message}");
        }
    }

    private async Task DisposeCityLensSurfaceAsync()
    {
        if (!_isCityLensSurfaceMounted)
        {
            return;
        }

        try
        {
            await JS.InvokeVoidAsync("visitorMap.dispose", "city-lens-map");
        }
        catch (JSException ex)
        {
            VisitorMobileDiagnostics.Log("CityLensRuntime", $"City Lens dispose failed: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            VisitorMobileDiagnostics.Log("CityLensRuntime", $"City Lens dispose skipped: {ex.Message}");
        }
        finally
        {
            _isCityLensSurfaceMounted = false;
        }
    }
}
