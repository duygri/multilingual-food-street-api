using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task LoadContentAsync(bool requestLocationPermission = false, bool preferNearbyPois = false)
    {
        var previousProximity = _state.ActiveProximity;
        _isContentLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await ContentService.LoadAsync(
                new VisitorContentLoadRequest(
                    PreferNearbyPois: preferNearbyPois,
                    RequestLocationPermission: requestLocationPermission));

            _state.UpdateLocation(result.Location);
            _state.ApplyContent(result.Content, result.IsFallback, result.SourceLabel, result.Message);
            var nextProximity = VisitorProximityEngine.Evaluate(result.Location, _state.Pois);
            await ApplyProximityNarrationAsync(previousProximity, nextProximity);
        }
        finally
        {
            _isContentLoading = false;
        }
    }
}
