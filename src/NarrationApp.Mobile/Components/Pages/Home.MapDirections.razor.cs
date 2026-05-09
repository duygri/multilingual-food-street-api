using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private string? GetWalkingDirectionsStatus()
    {
        return GetWalkingDirectionsNotice()?.StatusLabel;
    }

    private VisitorWalkingDirectionsNotice? GetWalkingDirectionsNotice()
    {
        return VisitorWalkingDirectionsPresentationFormatter.Build(
            _state.SelectedPoi,
            _walkingRoutePoiId,
            _walkingRoute,
            _walkingDirectionsStatus,
            _isWalkingRouteLoading);
    }

    private VisitorMapRoute? GetCurrentWalkingRoute()
    {
        return _state.SelectedPoi is not null
            && string.Equals(_walkingRoutePoiId, _state.SelectedPoi.Id, StringComparison.OrdinalIgnoreCase)
                ? _walkingRoute
                : null;
    }

    private async Task OpenSelectedPoiDirectionsAsync()
    {
        var selectedPoi = _state.SelectedPoi;
        if (selectedPoi is null)
        {
            return;
        }

        CloseNonContentSurfaces();
        ClearPoiAndTourDetailSelections();
        _state.OpenPoi(selectedPoi.Id);
        _walkingRoutePoiId = selectedPoi.Id;
        _walkingRoute = null;
        _walkingDirectionsStatus = null;
        _isWalkingRouteLoading = true;
        _mapRenderState.Reset();
        StateHasChanged();
        await Task.Yield();

        var result = await WalkingDirectionsService.LoadWalkingRouteAsync(_state.CurrentLocation, selectedPoi);
        if (!string.Equals(_state.SelectedPoiId, selectedPoi.Id, StringComparison.OrdinalIgnoreCase))
        {
            _isWalkingRouteLoading = false;
            StateHasChanged();
            return;
        }

        _isWalkingRouteLoading = false;
        _walkingDirectionsStatus = result.StatusLabel;
        _walkingRoute = result.Route;
        _mapRenderState.Reset();
        await RenderMapIfNeededAsync();
        StateHasChanged();
    }

    private async Task ClearWalkingDirectionsAsync()
    {
        _walkingRoutePoiId = null;
        _walkingRoute = null;
        _walkingDirectionsStatus = null;
        _isWalkingRouteLoading = false;
        _mapRenderState.Reset();
        await RenderMapIfNeededAsync();
        StateHasChanged();
    }
}
