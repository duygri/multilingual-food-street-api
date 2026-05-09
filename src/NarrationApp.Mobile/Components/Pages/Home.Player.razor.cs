namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void OpenFullPlayer()
    {
        if (_state.SelectedPoi is null || _state.CurrentAudioCue is null)
        {
            return;
        }

        CloseSearchOverlaySurface();
        _isFullPlayerOpen = true;
    }

    private void CloseFullPlayer()
    {
        CloseFullPlayerSurface();
    }

    private void ToggleFullPlayerTranscript()
    {
        _showFullPlayerLanguagePicker = false;
        _showFullPlayerTranscript = !_showFullPlayerTranscript;
    }

    private void ToggleFullPlayerLanguagePicker()
    {
        _showFullPlayerTranscript = false;
        _showFullPlayerLanguagePicker = !_showFullPlayerLanguagePicker;
    }
}
