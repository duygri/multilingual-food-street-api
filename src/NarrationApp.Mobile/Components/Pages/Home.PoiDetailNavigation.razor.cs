using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task OpenSelectedPoiDetailFromMapAsync()
    {
        if (_state.SelectedPoi is null)
        {
            return;
        }

        await OpenDiscoverPoiDetailCoreAsync(_state.SelectedPoi.Id);
    }

    private async Task OpenDiscoverPoiDetailCoreAsync(string poiId)
    {
        ShowDiscoverPoiDetail(poiId);
        _isAutoPlayingFromProximity = false;
        await PrepareSelectedPoiAudioAsync();
    }
}
