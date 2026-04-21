using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class TouristPendingDeepLinkStoreTests
{
    [Fact]
    public void SetPendingUri_StoresParsedQrRequest()
    {
        TouristPendingDeepLinkStore.Clear();

        TouristPendingDeepLinkStore.SetPendingUri("https://narration.app/qr/QR-001");
        var request = TouristPendingDeepLinkStore.Consume();

        Assert.NotNull(request);
        Assert.Equal("QR-001", request!.Code);
        Assert.Null(TouristPendingDeepLinkStore.Consume());
    }

    [Fact]
    public void SetPendingUri_IgnoresInvalidLinks()
    {
        TouristPendingDeepLinkStore.Clear();

        TouristPendingDeepLinkStore.SetPendingUri("https://narration.app/tours/river");

        Assert.Null(TouristPendingDeepLinkStore.Consume());
    }
}
