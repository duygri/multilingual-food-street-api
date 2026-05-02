using Microsoft.AspNetCore.Components;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void OpenSearchOverlay()
    {
        CloseFullPlayerSurface();
        _isSearchOverlayOpen = true;

        if (_state.ShowNotifications)
        {
            _state.ToggleNotifications();
        }
    }

    private void CloseSearchOverlay()
    {
        CloseSearchOverlaySurface();
    }

    private void SelectSearchCategory(string categoryId)
    {
        _state.SelectCategory(categoryId);
        _isSearchOverlayOpen = true;
    }

    private async Task OpenPoiDetailFromSearchAsync(string poiId)
    {
        _state.SwitchTab(VisitorTab.Discover);
        await OpenPoiDetailAsync(poiId);
    }

    private void OpenTourDetailFromSearch(string tourId)
    {
        _state.SwitchTab(VisitorTab.Tours);
        OpenTourDetail(tourId);
    }

    private void OnSearchChanged(ChangeEventArgs args)
    {
        _state.SetSearchTerm(args.Value?.ToString() ?? string.Empty);
        _isSearchOverlayOpen = true;
    }
}
