using Microsoft.JSInterop;

namespace NarrationApp.Web.Pages.Admin;

public partial class Analytics
{
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_pendingMapRender || _isLoading || !MapOptions.HasAccessToken)
        {
            return;
        }

        await RenderMapSurfaceAsync("adminAnalyticsMap.renderHeatmap", HeatmapContainerId, _heatmap);
        await RenderMapSurfaceAsync("adminAnalyticsMap.renderFlows", MovementContainerId, _movementFlows);
        _pendingMapRender = false;
    }

    public async ValueTask DisposeAsync()
    {
        if (!MapOptions.HasAccessToken)
        {
            return;
        }

        try
        {
            await JsRuntime.InvokeVoidAsync("adminAnalyticsMap.dispose", HeatmapContainerId);
            await JsRuntime.InvokeVoidAsync("adminAnalyticsMap.dispose", MovementContainerId);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private async Task RenderMapSurfaceAsync<TData>(string method, string containerId, IReadOnlyList<TData> items)
    {
        if (items.Count > 0)
        {
            await JsRuntime.InvokeVoidAsync(method, containerId, MapOptions.AccessToken, MapOptions.StyleUrl, items);
            return;
        }

        await JsRuntime.InvokeVoidAsync("adminAnalyticsMap.dispose", containerId);
    }
}
