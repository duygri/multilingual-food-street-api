namespace NarrationApp.Mobile.Features.Home;

public static class VisitorAutoNarrationDecider
{
    public static VisitorAutoNarrationDecision Evaluate(
        VisitorProximityMatch? previousProximity,
        VisitorProximityMatch? nextProximity,
        bool isAutoPlayingFromProximity,
        VisitorAudioPlaybackState playbackState)
    {
        var leftAutoNarrationZone = previousProximity is not null && nextProximity is null;
        if (!leftAutoNarrationZone)
        {
            return VisitorAutoNarrationDecision.None;
        }

        return new VisitorAutoNarrationDecision(
            ShouldPauseCurrentAudio: isAutoPlayingFromProximity && playbackState == VisitorAudioPlaybackState.Playing,
            ShouldResetAutoPlayedPoiId: true);
    }
}

public sealed record VisitorAutoNarrationDecision(bool ShouldPauseCurrentAudio, bool ShouldResetAutoPlayedPoiId)
{
    public static VisitorAutoNarrationDecision None { get; } = new(false, false);
}
