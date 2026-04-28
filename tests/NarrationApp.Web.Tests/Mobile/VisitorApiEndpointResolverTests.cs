using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorApiEndpointResolverTests
{
    [Fact]
    public void Parse_ReadsEnvironmentUrlsFromJson()
    {
        var options = VisitorApiOptions.Parse("""
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
        var uri = VisitorApiEndpointResolver.Resolve(
            new VisitorApiOptions
            {
                Development = new VisitorApiEnvironmentUrls
                {
                    Default = "https://localhost:5001",
                    Android = "https://10.0.2.2:5001"
                }
            },
            VisitorApiDeploymentEnvironment.Development,
            isAndroid: true);

        Assert.Equal("https://10.0.2.2:5001/", uri.ToString());
    }

    [Fact]
    public void Resolve_UsesDefaultDevelopmentUrl_WhenNotRunningOnAndroid()
    {
        var uri = VisitorApiEndpointResolver.Resolve(
            new VisitorApiOptions
            {
                Development = new VisitorApiEnvironmentUrls
                {
                    Default = "https://localhost:5001",
                    Android = "https://10.0.2.2:5001"
                }
            },
            VisitorApiDeploymentEnvironment.Development,
            isAndroid: false);

        Assert.Equal("https://localhost:5001/", uri.ToString());
    }

    [Fact]
    public void Resolve_FallsBackToDefaultUrl_WhenAndroidOverrideIsMissing()
    {
        var uri = VisitorApiEndpointResolver.Resolve(
            new VisitorApiOptions
            {
                Staging = new VisitorApiEnvironmentUrls
                {
                    Default = "https://staging-api.foodstreet.example"
                }
            },
            VisitorApiDeploymentEnvironment.Staging,
            isAndroid: true);

        Assert.Equal("https://staging-api.foodstreet.example/", uri.ToString());
    }

    [Fact]
    public void Resolve_ThrowsHelpfulError_WhenEnvironmentUrlIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            VisitorApiEndpointResolver.Resolve(
                new VisitorApiOptions(),
                VisitorApiDeploymentEnvironment.Production,
                isAndroid: false));

        Assert.Contains("Production", exception.Message, StringComparison.Ordinal);
    }
}
