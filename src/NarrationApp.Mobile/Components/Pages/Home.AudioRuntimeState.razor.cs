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

            await JS.InvokeVoidAsync("visitorAudio.preload", cue.StreamUrl, _audioBridge);
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
            await JS.InvokeVoidAsync("visitorAudio.play", cue.StreamUrl, _audioBridge);
            var playbackLabel = forceAutoPlay
                ? $"Đang phát từ QR • {cue.LanguageCode.ToUpperInvariant()}"
                : $"Đang phát tự động • {cue.LanguageCode.ToUpperInvariant()}";
            _state.SetAudioPlaybackState(VisitorAudioPlaybackState.Playing, playbackLabel);
            await TrackAudioPlayAsync(cue);
        }
        catch (JSException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioRuntime] JS interop failed: {ex.Message}");
            _state.SetAudioPlaybackState(VisitorAudioPlaybackState.Error, "Audio chưa sẵn sàng");
        }
    }
}
