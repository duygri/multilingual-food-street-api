namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void SelectTour(string tourId)
    {
        _state.SelectTour(tourId);
    }

    private async Task StartSelectedTourAsync()
    {
        if (_state.SelectedTour is null)
        {
            return;
        }

        await StartTourAsync(_state.SelectedTour.Id);
    }

    private async Task StartTourAsync(string tourId)
    {
        CloseTourDetailSelection();
        CloseDiscoverPoiDetailSelection();
        _lastAutoPlayedPoiId = null;
        _isAutoPlayingFromProximity = false;
        _state.StartTour(tourId);

        if (_state.SelectedPoi is not null)
        {
            await PrepareSelectedPoiAudioAsync();
        }
    }
}
