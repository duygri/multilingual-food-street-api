using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task PlaySelectedPoiAsync()
    {
        if (_state.SelectedPoi is null)
        {
            return;
        }

        if (ShouldPrepareSelectedPoiAudio())
        {
            await PrepareSelectedPoiAudioAsync();
        }

        if (!_state.CanPlayAudio || _state.CurrentAudioCue is null)
        {
            return;
        }

        try
        {
            _isAutoPlayingFromProximity = false;
            var playbackUrl = await VisitorAudioPlaybackSourceResolver.ResolveAsync(_state.CurrentAudioCue.StreamUrl, JS);
            await JS.InvokeVoidAsync("visitorAudio.play", playbackUrl, _audioBridge);
            await JS.InvokeVoidAsync("visitorAudio.setRate", AudioSpeedOptions[_audioSpeedIndex]);
            _state.SetAudioPlaybackState(VisitorAudioPlaybackState.Playing, UiText.FormatPlayingLabel(_state.CurrentAudioCue.LanguageCode));
            await TrackAudioPlayAsync(_state.CurrentAudioCue);
        }
        catch (Exception ex)
        {
            VisitorMobileDiagnostics.Log("AudioPlayback", $"Playback source failed: {ex.Message}");
            _state.SetAudioPlaybackState(VisitorAudioPlaybackState.Error, UiText.AudioPlaybackFailedLabel());
        }
    }

    private async Task TogglePlaybackAsync()
    {
        if (_state.IsAudioPlaying)
        {
            try
            {
                await JS.InvokeVoidAsync("visitorAudio.pause");
                _state.SetAudioPlaybackState(VisitorAudioPlaybackState.Paused, UiText.AudioPausedLabel());
            }
            catch (JSException ex)
            {
                VisitorMobileDiagnostics.Log("AudioPlayback", $"JS pause failed: {ex.Message}");
                _state.SetAudioPlaybackState(VisitorAudioPlaybackState.Error, UiText.AudioNotReadyLabel());
            }

            return;
        }

        await PlaySelectedPoiAsync();
    }

    private string GetMiniPlayIcon() => _state.IsAudioPlaying ? "❚❚" : "▶";

    private Task TrackAudioPlayAsync(VisitorAudioCue cue)
    {
        return AudioPlayReporter.TrackAsync(cue, _state.CurrentLocation);
    }
}
