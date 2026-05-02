using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorMapOptionsTests
{
    [Fact]
    public void Default_map_style_prefers_light_theme_for_public_visitor_map()
    {
        var options = new VisitorMapOptions();

        Assert.Equal("mapbox://styles/mapbox/light-v11", options.StyleUrl);
        Assert.Equal("YOUR_MAPBOX_ACCESS_TOKEN_HERE", options.AccessToken);
    }
}
