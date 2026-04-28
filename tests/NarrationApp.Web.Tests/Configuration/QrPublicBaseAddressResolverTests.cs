using NarrationApp.Web.Configuration;

namespace NarrationApp.Web.Tests.Configuration;

public sealed class QrPublicBaseAddressResolverTests
{
    [Fact]
    public void Resolve_returns_absolute_configured_url_when_present()
    {
        var result = QrPublicBaseAddressResolver.Resolve("https://narration.app/", "https://web.foodstreet.test/");

        Assert.Equal(new Uri("https://narration.app/"), result);
    }

    [Fact]
    public void Resolve_uses_host_base_address_for_blank_config()
    {
        var result = QrPublicBaseAddressResolver.Resolve("", "https://web.foodstreet.test/");

        Assert.Equal(new Uri("https://web.foodstreet.test/"), result);
    }

    [Fact]
    public void Resolve_supports_relative_public_paths()
    {
        var result = QrPublicBaseAddressResolver.Resolve("/share/", "https://web.foodstreet.test/");

        Assert.Equal(new Uri("https://web.foodstreet.test/share/"), result);
    }
}
