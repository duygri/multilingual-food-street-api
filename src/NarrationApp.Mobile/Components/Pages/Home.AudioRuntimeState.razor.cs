using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void RestorePreparedAudioState(
        VisitorAudioCue cue,
        bool isSameCue,
        int previousElapsedSeconds,
        int previousDurationSeconds,
        VisitorAudioPlaybackState previousPlaybackState)
    {
        if (!isSameCue)
        {
            return;
        }

        _state.UpdateAudioProgress(previousElapsedSeconds, previousDurationSeconds);

        var restoredPlaybackState = previousPlaybackState switch
        {
            VisitorAudioPlaybackState.Playing => VisitorAudioPlaybackState.Playing,
            VisitorAudioPlaybackState.Paused => VisitorAudioPlaybackState.Paused,
            VisitorAudioPlaybackState.Ready => VisitorAudioPlaybackState.Ready,
            _ => VisitorAudioPlaybackState.Ready
        };

        _state.SetAudioPlaybackState(restoredPlaybackState, cue.StatusLabel);
    }

    private async Task FinalizePreparedAudioCueAsync(VisitorAudioCue cue, bool autoPlay, bool forceAutoPlay)
    {
        try
        {
            if (!cue.IsAvailable)
            {
                await JS.InvokeVoidAsync("visitorAudio.dispose");
                return;
            }

            var playbackUrl = await VisitorAudioPlaybackSourceResolver.ResolveAsync(cue.StreamUrl, JS);
            await JS.InvokeVoidAsync("visitorAudio.preload", playbackUrl, _audioBridge);
            await JS.InvokeVoidAsync("visitorAudio.setRate", AudioSpeedOptions[_audioSpeedIndex]);

            var shouldAutoplay = ShouldAutoplayCue(cue, autoPlay, forceAutoPlay);

            VisitorMobileDiagnostics.Log(
                "Audio",
                $"PrepareSelectedPoiAudioAsync poi={cue.PoiId} autoPlayRequested={autoPlay} forceAutoPlay={forceAutoPlay} activeProximity={_state.ActiveProximity?.PoiId ?? "<null>"} lastAutoPlayed={_lastAutoPlayedPoiId ?? "<null>"} shouldAutoplay={shouldAutoplay}");

            if (!shouldAutoplay)
            {
                return;
            }

            MarkAutoNarrationStarted(cue, forceAutoPlay);
            await JS.InvokeVoidAsync("visitorAudio.play", playbackUrl, _audioBridge);
            var playbackLabel = forceAutoPlay
                ? UiText.FormatQrPlayingLabel(cue.LanguageCode)
                : UiText.FormatAutoPlayingLabel(cue.LanguageCode);
            _state.SetAudioPlaybackState(VisitorAudioPlaybackState.Playing, playbackLabel);
            await TrackAudioPlayAsync(cue);
        }
        catch (Exception ex)
        {
            VisitorMobileDiagnostics.Log("AudioRuntime", $"Playback source failed: {ex.Message}");
            _state.SetAudioPlaybackState(VisitorAudioPlaybackState.Error, UiText.AudioNotReadyLabel());
        }
    }
}
