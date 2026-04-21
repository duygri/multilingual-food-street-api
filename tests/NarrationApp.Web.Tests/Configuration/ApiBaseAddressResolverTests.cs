using NarrationApp.Web.Configuration;

namespace NarrationApp.Web.Tests.Configuration;

public sealed class ApiBaseAddressResolverTests
{
    [Fact]
    public void Resolve_returns_absolute_configured_url_when_present()
    {
        var result = ApiBaseAddressResolver.Resolve("https://api.foodstreet.test/", "https://web.foodstreet.test/");

        Assert.Equal(new Uri("https://api.foodstreet.test/"), result);
    }

    [Fact]
    public void Resolve_uses_host_base_address_for_blank_config()
    {
        var result = ApiBaseAddressResolver.Resolve("", "https://web.foodstreet.test/");

        Assert.Equal(new Uri("https://web.foodstreet.test/"), result);
    }

    [Fact]
    public void Resolve_supports_relative_api_paths()
    {
        var result = ApiBaseAddressResolver.Resolve("/api/", "https://web.foodstreet.test/");

        Assert.Equal(new Uri("https://web.foodstreet.test/api/"), result);
    }
}
