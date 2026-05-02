using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private bool ShouldAutoplayCue(VisitorAudioCue cue, bool autoPlay, bool forceAutoPlay) =>
        VisitorAudioAutoplayPolicy.ShouldAutoplay(
            autoPlayRequested: autoPlay,
            forceAutoPlay: forceAutoPlay,
            cuePoiId: cue.PoiId,
            activeProximityPoiId: _state.ActiveProximity?.PoiId,
            lastAutoPlayedPoiId: _lastAutoPlayedPoiId,
            lastAutoPlayedAtUtc: GetLastAutoNarrationPlayedAt(cue.PoiId),
            nowUtc: DateTimeOffset.UtcNow,
            cooldownWindow: AutoNarrationCooldownWindow,
            hasAutoNarrationLock: _isAutoPlayingFromProximity && _state.AudioPlaybackState == VisitorAudioPlaybackState.Playing,
            currentAutoNarrationPoiId: _currentAutoNarrationPoiId);

    private void MarkAutoNarrationStarted(VisitorAudioCue cue, bool forceAutoPlay)
    {
        _lastAutoPlayedPoiId = cue.PoiId;
        _isAutoPlayingFromProximity = !forceAutoPlay;
        RecordAutoNarrationPlayback(cue.PoiId, DateTimeOffset.UtcNow);
    }
}
