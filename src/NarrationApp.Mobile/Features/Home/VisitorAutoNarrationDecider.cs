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
        var switchedActivePoi =
            previousProximity is not null
            && nextProximity is not null
            && !string.Equals(previousProximity.PoiId, nextProximity.PoiId, StringComparison.OrdinalIgnoreCase);

        if (!leftAutoNarrationZone && !switchedActivePoi)
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
