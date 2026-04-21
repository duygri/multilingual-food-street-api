using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class TouristApiEndpointResolverTests
{
    [Fact]
    public void Parse_ReadsEnvironmentUrlsFromJson()
    {
        var options = TouristApiOptions.Parse("""
            {
              "development": {
                "default": "https://localhost:5001/",
                "android": "https://10.0.2.2:5001/"
              },
              "staging": {
                "default": "https://staging-api.foodstreet.example/"
              },
              "production": {
                "default": "https://api.foodstreet.example/"
              }
            }
            """);

        Assert.Equal("https://localhost:5001/", options.Development.Default);
        Assert.Equal("https://10.0.2.2:5001/", options.Development.Android);
        Assert.Equal("https://api.foodstreet.example/", options.Production.Default);
    }

    [Fact]
    public void Resolve_UsesAndroidDevelopmentUrl_WhenRunningOnAndroid()
    {
        var uri = TouristApiEndpointResolver.Resolve(
            new TouristApiOptions
            {
                Development = new TouristApiEnvironmentUrls
                {
                    Default = "https://localhost:5001",
                    Android = "https://10.0.2.2:5001"
                }
            },
            TouristApiDeploymentEnvironment.Development,
            isAndroid: true);

        Assert.Equal("https://10.0.2.2:5001/", uri.ToString());
    }

    [Fact]
    public void Resolve_UsesDefaultDevelopmentUrl_WhenNotRunningOnAndroid()
    {
        var uri = TouristApiEndpointResolver.Resolve(
            new TouristApiOptions
            {
                Development = new TouristApiEnvironmentUrls
                {
                    Default = "https://localhost:5001",
                    Android = "https://10.0.2.2:5001"
                }
            },
            TouristApiDeploymentEnvironment.Development,
            isAndroid: false);

        Assert.Equal("https://localhost:5001/", uri.ToString());
    }

    [Fact]
    public void Resolve_FallsBackToDefaultUrl_WhenAndroidOverrideIsMissing()
    {
        var uri = TouristApiEndpointResolver.Resolve(
            new TouristApiOptions
            {
                Staging = new TouristApiEnvironmentUrls
                {
                    Default = "https://staging-api.foodstreet.example"
                }
            },
            TouristApiDeploymentEnvironment.Staging,
            isAndroid: true);

        Assert.Equal("https://staging-api.foodstreet.example/", uri.ToString());
    }

    [Fact]
    public void Resolve_ThrowsHelpfulError_WhenEnvironmentUrlIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            TouristApiEndpointResolver.Resolve(
                new TouristApiOptions(),
                TouristApiDeploymentEnvironment.Production,
                isAndroid: false));

        Assert.Contains("Production", exception.Message, StringComparison.Ordinal);
    }
}
