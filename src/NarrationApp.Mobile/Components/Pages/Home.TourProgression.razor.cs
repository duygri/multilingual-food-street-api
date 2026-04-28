namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task OpenNextTourStopAsync()
    {
        if (_state.ActiveTourSession?.NextPoiId is null)
        {
            return;
        }

        _tourDetailId = null;
        _state.OpenPoi(_state.ActiveTourSession.NextPoiId);
        _lastAutoPlayedPoiId = null;
        _isAutoPlayingFromProximity = false;
        await PrepareSelectedPoiAudioAsync();
    }

    private async Task AdvanceActiveTourAsync()
    {
        if (_state.SelectedPoi is null)
        {
            return;
        }

        _lastAutoPlayedPoiId = null;
        _isAutoPlayingFromProximity = false;

        var advanced = _state.AdvanceActiveTour(_state.SelectedPoi.Id);
        if (!advanced)
        {
            return;
        }

        if (_state.ActiveTourSession?.IsCompleted == false && _state.SelectedPoi is not null)
        {
            await PrepareSelectedPoiAudioAsync();
        }
    }
}
