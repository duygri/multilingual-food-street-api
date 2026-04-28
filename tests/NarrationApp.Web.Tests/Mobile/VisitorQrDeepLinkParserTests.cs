using NarrationApp.Mobile.Features.Home;
using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorQrDeepLinkParserTests
{
    [Fact]
    public void TryParse_AcceptsHostedQrLink()
    {
        var success = VisitorQrDeepLinkParser.TryParse(
            "https://public.foodstreet.test/qr/QR-001",
            out var request);

        Assert.True(success);
        Assert.NotNull(request);
        Assert.Equal("QR-001", request!.Code);
        Assert.Equal("https://public.foodstreet.test/qr/QR-001", request.SourceUri);
    }

    [Fact]
    public void TryParse_AcceptsCustomSchemeQrLink()
    {
        var success = VisitorQrDeepLinkParser.TryParse(
            "foodstreet://qr/QR-TOUR-7",
            out var request);

        Assert.True(success);
        Assert.NotNull(request);
        Assert.Equal("QR-TOUR-7", request!.Code);
    }

    [Fact]
    public void TryParse_RejectsNonQrLinks()
    {
        var success = VisitorQrDeepLinkParser.TryParse(
            "https://narration.app/tours/river-walk",
            out var request);

        Assert.False(success);
        Assert.Null(request);
    }

    [Fact]
    public void FromQrCodeDto_MapsPoiTargetToMobileId()
    {
        var target = VisitorQrNavigationTarget.FromQrCode(new QrCodeDto
        {
            Code = "QR-001",
            TargetType = "poi",
            TargetId = 7
        });

        Assert.Equal(VisitorQrTargetKind.Poi, target.Kind);
        Assert.Equal("poi-7", target.TargetId);
        Assert.Equal("QR-001", target.Code);
    }

    [Fact]
    public void FromQrCodeDto_FallsBackToOpenAppForUnsupportedTourTarget()
    {
        var target = VisitorQrNavigationTarget.FromQrCode(new QrCodeDto
        {
            Code = "QR-TOUR-2",
            TargetType = "tour",
            TargetId = 2
        });

        Assert.Equal(VisitorQrTargetKind.OpenApp, target.Kind);
        Assert.Null(target.TargetId);
    }

    [Fact]
    public void FromQrCodeDto_FallsBackToOpenAppForUnknownTargets()
    {
        var target = VisitorQrNavigationTarget.FromQrCode(new QrCodeDto
        {
            Code = "QR-APP-1",
            TargetType = "something-else",
            TargetId = 0
        });

        Assert.Equal(VisitorQrTargetKind.OpenApp, target.Kind);
        Assert.Null(target.TargetId);
    }
}
