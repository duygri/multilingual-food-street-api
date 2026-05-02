using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorMapDirectionsLinkBuilderTests
{
    [Fact]
    public void BuildDirectionsUrl_TargetsGoogleMapsWithPoiCoordinates()
    {
        var poi = new VisitorPoi(
            "poi-oc-oanh",
            "Ốc Oanh",
            "hai-san",
            "Hải sản",
            "Quận 4",
            "Live API",
            "POI directions test",
            "Hải sản nổi bật",
            32,
            48,
            140,
            "2:40",
            "Sẵn sàng",
            10.7607,
            106.7033);

        var url = VisitorMapDirectionsLinkBuilder.BuildDirectionsUrl(poi);

        Assert.Equal(
            "https://www.google.com/maps/dir/?api=1&destination=10.7607%2C106.7033&travelmode=walking",
            url);
    }
}
