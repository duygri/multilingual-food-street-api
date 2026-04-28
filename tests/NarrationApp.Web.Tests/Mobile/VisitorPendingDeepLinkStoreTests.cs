using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorPendingDeepLinkStoreTests
{
    [Fact]
    public void SetPendingUri_StoresParsedQrRequest()
    {
        VisitorPendingDeepLinkStore.Clear();

        VisitorPendingDeepLinkStore.SetPendingUri("https://narration.app/qr/QR-001");
        var request = VisitorPendingDeepLinkStore.Consume();

        Assert.NotNull(request);
        Assert.Equal("QR-001", request!.Code);
        Assert.Null(VisitorPendingDeepLinkStore.Consume());
    }

    [Fact]
    public void SetPendingUri_IgnoresInvalidLinks()
    {
        VisitorPendingDeepLinkStore.Clear();

        VisitorPendingDeepLinkStore.SetPendingUri("https://narration.app/tours/river");

        Assert.Null(VisitorPendingDeepLinkStore.Consume());
    }
}
