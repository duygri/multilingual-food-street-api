namespace NarrationApp.Mobile.Features.Home;

public static class TouristAutoNarrationDecider
{
    public static TouristAutoNarrationDecision Evaluate(
        TouristProximityMatch? previousProximity,
        TouristProximityMatch? nextProximity,
        bool isAutoPlayingFromProximity,
        TouristAudioPlaybackState playbackState)
    {
        var leftAutoNarrationZone = previousProximity is not null && nextProximity is null;
        if (!leftAutoNarrationZone)
        {
            return TouristAutoNarrationDecision.None;
        }

        return new TouristAutoNarrationDecision(
            ShouldPauseCurrentAudio: isAutoPlayingFromProximity && playbackState == TouristAudioPlaybackState.Playing,
            ShouldResetAutoPlayedPoiId: true);
    }
}

public sealed record TouristAutoNarrationDecision(bool ShouldPauseCurrentAudio, bool ShouldResetAutoPlayedPoiId)
{
    public static TouristAutoNarrationDecision None { get; } = new(false, false);
}
