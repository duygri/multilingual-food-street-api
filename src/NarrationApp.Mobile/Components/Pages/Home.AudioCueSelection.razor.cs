using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private bool ShouldPrepareSelectedPoiAudio()
    {
        return _state.SelectedPoi is not null
            && (_state.CurrentAudioCue is null
                || _state.CurrentAudioCue.PoiId != _state.SelectedPoi.Id
                || !string.Equals(
                    _state.CurrentAudioCue.LanguageCode,
                    _state.SelectedLanguageCode,
                    StringComparison.OrdinalIgnoreCase));
    }
}
