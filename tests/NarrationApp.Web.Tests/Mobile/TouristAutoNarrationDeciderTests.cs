using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class TouristAutoNarrationDeciderTests
{
    [Fact]
    public void Evaluate_ReturnsPauseAndResetWhenLeavingZoneDuringAutoPlayback()
    {
        var decision = TouristAutoNarrationDecider.Evaluate(
            previousProximity: new TouristProximityMatch("poi-12", "Bến Nhà Rồng", 24, 120),
            nextProximity: null,
            isAutoPlayingFromProximity: true,
            playbackState: TouristAudioPlaybackState.Playing);

        Assert.True(decision.ShouldPauseCurrentAudio);
        Assert.True(decision.ShouldResetAutoPlayedPoiId);
    }

    [Fact]
    public void Evaluate_DoesNotPauseManualPlaybackWhenLeavingZone()
    {
        var decision = TouristAutoNarrationDecider.Evaluate(
            previousProximity: new TouristProximityMatch("poi-12", "Bến Nhà Rồng", 24, 120),
            nextProximity: null,
            isAutoPlayingFromProximity: false,
            playbackState: TouristAudioPlaybackState.Playing);

        Assert.False(decision.ShouldPauseCurrentAudio);
        Assert.True(decision.ShouldResetAutoPlayedPoiId);
    }

    [Fact]
    public void Evaluate_DoesNothingWhenStillInsideAnyZone()
    {
        var decision = TouristAutoNarrationDecider.Evaluate(
            previousProximity: new TouristProximityMatch("poi-12", "Bến Nhà Rồng", 24, 120),
            nextProximity: new TouristProximityMatch("poi-15", "Chợ Bến Thành", 18, 120),
            isAutoPlayingFromProximity: true,
            playbackState: TouristAudioPlaybackState.Playing);

        Assert.False(decision.ShouldPauseCurrentAudio);
        Assert.False(decision.ShouldResetAutoPlayedPoiId);
    }
}
