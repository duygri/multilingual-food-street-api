using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void SelectDiscoverCategory(string categoryId)
    {
        _discoverPoiDetailId = null;
        _state.SelectCategory(categoryId);
    }

    private async Task OpenPoiDetailAsync(string poiId)
    {
        _isQrModalOpen = false;
        _isSearchOverlayOpen = false;
        _isFullPlayerOpen = false;
        _state.PreviewPoi(poiId);
        _discoverPoiDetailId = poiId;
        _tourDetailId = null;
        _isAutoPlayingFromProximity = false;
        await PrepareSelectedPoiAudioAsync();
    }

    private void ClosePoiDetail()
    {
        _discoverPoiDetailId = null;
    }

    private async Task OpenSelectedPoiOnMapAsync()
    {
        if (_state.SelectedPoi is null)
        {
            return;
        }

        _isFullPlayerOpen = false;
        _discoverPoiDetailId = null;
        _state.OpenPoi(_state.SelectedPoi.Id);
        _isAutoPlayingFromProximity = false;
        await PrepareSelectedPoiAudioAsync();
    }

    private async Task SelectLanguageFromDetailAsync(string languageCode)
    {
        await SelectAudioLanguageAsync(languageCode, keepPlayback: false);
    }

    private IReadOnlyList<VisitorPoi> GetRelatedPois() =>
        VisitorRelatedPoiSelector.Select(_state.Pois, _state.SelectedPoi);

    private async Task OpenPoiFromDiscoverAsync(string poiId)
    {
        _state.OpenPoi(poiId);
        _isAutoPlayingFromProximity = false;
        await PrepareSelectedPoiAudioAsync();
    }
}
