using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorAudioAutoplayPolicyTests
{
    [Fact]
    public void ForceAutoPlay_AllowsDeepLinkPlaybackWithoutActiveProximity()
    {
        var shouldAutoplay = VisitorAudioAutoplayPolicy.ShouldAutoplay(
            autoPlayRequested: true,
            forceAutoPlay: true,
            cuePoiId: "poi-6",
            activeProximityPoiId: null,
            lastAutoPlayedPoiId: null);

        Assert.True(shouldAutoplay);
    }

    [Fact]
    public void AutoPlayWithoutForce_StillRequiresMatchingActiveProximity()
    {
        var shouldAutoplay = VisitorAudioAutoplayPolicy.ShouldAutoplay(
            autoPlayRequested: true,
            forceAutoPlay: false,
            cuePoiId: "poi-6",
            activeProximityPoiId: null,
            lastAutoPlayedPoiId: null);

        Assert.False(shouldAutoplay);
    }

    [Fact]
    public void AutoPlay_SkipsCueThatWasAlreadyAutoPlayed()
    {
        var shouldAutoplay = VisitorAudioAutoplayPolicy.ShouldAutoplay(
            autoPlayRequested: true,
            forceAutoPlay: true,
            cuePoiId: "poi-6",
            activeProximityPoiId: "poi-6",
            lastAutoPlayedPoiId: "poi-6");

        Assert.False(shouldAutoplay);
    }
}
