using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void CloseSearchOverlaySurface()
    {
        _isSearchOverlayOpen = false;
    }

    private void CloseFullPlayerSurface()
    {
        _isFullPlayerOpen = false;
        _showFullPlayerTranscript = false;
    }

    private void CloseNonContentSurfaces()
    {
        CloseSearchOverlaySurface();
        CloseFullPlayerSurface();
    }

    private void ClearPoiAndTourDetailSelections()
    {
        CloseDiscoverPoiDetailSelection();
        CloseTourDetailSelection();
    }

    private void CloseDiscoverPoiDetailSelection()
    {
        _discoverPoiDetailId = null;
    }

    private void CloseTourDetailSelection()
    {
        _tourDetailId = null;
    }

    private void PrepareForPrimaryTabSwitch()
    {
        CloseNonContentSurfaces();
        ClearPoiAndTourDetailSelections();
        _state.CloseSettingsScreen();
    }

    private void ShowDiscoverPoiDetail(string poiId)
    {
        CloseNonContentSurfaces();
        CloseTourDetailSelection();
        _state.SwitchTab(VisitorTab.Discover);
        _state.PreviewPoi(poiId);
        _discoverPoiDetailId = poiId;
    }
}
