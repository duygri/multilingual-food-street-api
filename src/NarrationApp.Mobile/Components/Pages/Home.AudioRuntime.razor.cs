using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task PrepareSelectedPoiAudioAsync(bool autoPlay = false, bool forceAutoPlay = false)
    {
        if (_state.SelectedPoi is null)
        {
            return;
        }

        var previousCue = _state.CurrentAudioCue;
        var previousElapsedSeconds = _state.AudioElapsedSeconds;
        var previousDurationSeconds = _state.AudioDurationSeconds;
        var previousPlaybackState = _state.AudioPlaybackState;

        _state.SetAudioPlaybackState(VisitorAudioPlaybackState.Loading, "Đang kiểm tra audio...");
        var cue = await AudioCatalogService.LoadBestForPoiAsync(_state.SelectedPoi.Id, _state.SelectedLanguageCode);
        var isSameCue =
            previousCue is not null
            && cue.IsAvailable
            && previousCue.IsAvailable
            && previousCue.PoiId == cue.PoiId
            && string.Equals(previousCue.StreamUrl, cue.StreamUrl, StringComparison.OrdinalIgnoreCase);

        _state.SetAudioCue(cue);
        RestorePreparedAudioState(cue, isSameCue, previousElapsedSeconds, previousDurationSeconds, previousPlaybackState);
        await FinalizePreparedAudioCueAsync(cue, autoPlay, forceAutoPlay);
    }
}
