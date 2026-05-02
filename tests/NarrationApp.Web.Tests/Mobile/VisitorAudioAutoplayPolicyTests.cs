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

    [Fact]
    public void AutoPlay_SkipsCueInsideCooldownWindow()
    {
        var shouldAutoplay = VisitorAudioAutoplayPolicy.ShouldAutoplay(
            autoPlayRequested: true,
            forceAutoPlay: true,
            cuePoiId: "poi-6",
            activeProximityPoiId: "poi-6",
            lastAutoPlayedPoiId: null,
            lastAutoPlayedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-2),
            nowUtc: DateTimeOffset.UtcNow,
            cooldownWindow: TimeSpan.FromMinutes(5),
            hasAutoNarrationLock: false,
            currentAutoNarrationPoiId: null);

        Assert.False(shouldAutoplay);
    }

    [Fact]
    public void AutoPlay_SkipsDifferentCueWhileAnotherAutoNarrationIsPlaying()
    {
        var shouldAutoplay = VisitorAudioAutoplayPolicy.ShouldAutoplay(
            autoPlayRequested: true,
            forceAutoPlay: false,
            cuePoiId: "poi-7",
            activeProximityPoiId: "poi-7",
            lastAutoPlayedPoiId: null,
            lastAutoPlayedAtUtc: null,
            nowUtc: DateTimeOffset.UtcNow,
            cooldownWindow: TimeSpan.FromMinutes(5),
            hasAutoNarrationLock: true,
            currentAutoNarrationPoiId: "poi-6");

        Assert.False(shouldAutoplay);
    }
}
