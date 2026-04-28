using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Web.Configuration;

namespace NarrationApp.Web.Tests.Configuration;

public sealed class QrPublicUrlOptionsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddQrPublicUrlOptions_registers_resolved_options_for_components()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QrPublicBaseUrl"] = "/share/"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddQrPublicUrlOptions(configuration, "https://web.foodstreet.test/");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<QrPublicUrlOptions>();

        Assert.Equal(new Uri("https://web.foodstreet.test/share/"), options.BaseAddress);
    }
}
