using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorAutoNarrationDeciderTests
{
    [Fact]
    public void Evaluate_ReturnsPauseAndResetWhenLeavingZoneDuringAutoPlayback()
    {
        var decision = VisitorAutoNarrationDecider.Evaluate(
            previousProximity: new VisitorProximityMatch("poi-12", "Bến Nhà Rồng", 24, 120),
            nextProximity: null,
            isAutoPlayingFromProximity: true,
            playbackState: VisitorAudioPlaybackState.Playing);

        Assert.True(decision.ShouldPauseCurrentAudio);
        Assert.True(decision.ShouldResetAutoPlayedPoiId);
    }

    [Fact]
    public void Evaluate_DoesNotPauseManualPlaybackWhenLeavingZone()
    {
        var decision = VisitorAutoNarrationDecider.Evaluate(
            previousProximity: new VisitorProximityMatch("poi-12", "Bến Nhà Rồng", 24, 120),
            nextProximity: null,
            isAutoPlayingFromProximity: false,
            playbackState: VisitorAudioPlaybackState.Playing);

        Assert.False(decision.ShouldPauseCurrentAudio);
        Assert.True(decision.ShouldResetAutoPlayedPoiId);
    }

    [Fact]
    public void Evaluate_DoesNothingWhenStillInsideSamePoiZone()
    {
        var decision = VisitorAutoNarrationDecider.Evaluate(
            previousProximity: new VisitorProximityMatch("poi-12", "Bến Nhà Rồng", 24, 120),
            nextProximity: new VisitorProximityMatch("poi-12", "Bến Nhà Rồng", 18, 120),
            isAutoPlayingFromProximity: true,
            playbackState: VisitorAudioPlaybackState.Playing);

        Assert.False(decision.ShouldPauseCurrentAudio);
        Assert.False(decision.ShouldResetAutoPlayedPoiId);
    }

    [Fact]
    public void Evaluate_PausesAndResetsWhenSwitchingToDifferentPoiDuringAutoPlayback()
    {
        var decision = VisitorAutoNarrationDecider.Evaluate(
            previousProximity: new VisitorProximityMatch("poi-12", "Bến Nhà Rồng", 24, 120),
            nextProximity: new VisitorProximityMatch("poi-15", "Chợ Bến Thành", 18, 120),
            isAutoPlayingFromProximity: true,
            playbackState: VisitorAudioPlaybackState.Playing);

        Assert.True(decision.ShouldPauseCurrentAudio);
        Assert.True(decision.ShouldResetAutoPlayedPoiId);
    }
}
