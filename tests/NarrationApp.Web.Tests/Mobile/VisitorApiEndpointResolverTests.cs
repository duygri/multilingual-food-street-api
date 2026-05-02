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
                "android": "https://10.0.2.2:5001/",
                "androidEmulator": "https://10.0.2.2:5001/",
                "androidDevice": "https://192.168.98.219:5001/"
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
        Assert.Equal("https://10.0.2.2:5001/", options.Development.AndroidEmulator);
        Assert.Equal("https://192.168.98.219:5001/", options.Development.AndroidDevice);
        Assert.Equal("https://api.foodstreet.example/", options.Production.Default);
    }

    [Fact]
    public void Resolve_UsesAndroidEmulatorUrl_WhenRunningOnAndroidEmulator()
    {
        var uri = VisitorApiEndpointResolver.Resolve(
            new VisitorApiOptions
            {
                Development = new VisitorApiEnvironmentUrls
                {
                    Default = "https://localhost:5001",
                    Android = "https://fallback-android:5001",
                    AndroidEmulator = "https://10.0.2.2:5001",
                    AndroidDevice = "https://192.168.98.219:5001"
                }
            },
            VisitorApiDeploymentEnvironment.Development,
            VisitorApiClientPlatform.AndroidEmulator);

        Assert.Equal("https://10.0.2.2:5001/", uri.ToString());
    }

    [Fact]
    public void Resolve_UsesAndroidDeviceUrl_WhenRunningOnPhysicalAndroid()
    {
        var uri = VisitorApiEndpointResolver.Resolve(
            new VisitorApiOptions
            {
                Development = new VisitorApiEnvironmentUrls
                {
                    Default = "https://localhost:5001",
                    Android = "https://fallback-android:5001",
                    AndroidEmulator = "https://10.0.2.2:5001",
                    AndroidDevice = "https://192.168.98.219:5001"
                }
            },
            VisitorApiDeploymentEnvironment.Development,
            VisitorApiClientPlatform.AndroidDevice);

        Assert.Equal("https://192.168.98.219:5001/", uri.ToString());
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
            VisitorApiClientPlatform.Default);

        Assert.Equal("https://localhost:5001/", uri.ToString());
    }

    [Fact]
    public void Resolve_FallsBackToGenericAndroidUrl_WhenSpecificAndroidOverrideIsMissing()
    {
        var uri = VisitorApiEndpointResolver.Resolve(
            new VisitorApiOptions
            {
                Staging = new VisitorApiEnvironmentUrls
                {
                    Default = "https://staging-api.foodstreet.example",
                    Android = "https://staging-android.foodstreet.example"
                }
            },
            VisitorApiDeploymentEnvironment.Staging,
            VisitorApiClientPlatform.AndroidDevice);

        Assert.Equal("https://staging-android.foodstreet.example/", uri.ToString());
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
            VisitorApiClientPlatform.AndroidDevice);

        Assert.Equal("https://staging-api.foodstreet.example/", uri.ToString());
    }

    [Fact]
    public void Resolve_FallsBackToDevelopmentAndroidDeviceUrl_WhenNonDevelopmentEndpointIsPlaceholder()
    {
        var uri = VisitorApiEndpointResolver.Resolve(
            new VisitorApiOptions
            {
                Development = new VisitorApiEnvironmentUrls
                {
                    Default = "http://localhost:5000",
                    AndroidDevice = "http://192.168.98.219:5000"
                },
                Production = new VisitorApiEnvironmentUrls
                {
                    Default = "https://api.foodstreet.example"
                }
            },
            VisitorApiDeploymentEnvironment.Production,
            VisitorApiClientPlatform.AndroidDevice);

        Assert.Equal("http://192.168.98.219:5000/", uri.ToString());
    }

    [Fact]
    public void Resolve_ThrowsHelpfulError_WhenEnvironmentUrlIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            VisitorApiEndpointResolver.Resolve(
                new VisitorApiOptions(),
                VisitorApiDeploymentEnvironment.Production,
                VisitorApiClientPlatform.Default));

        Assert.Contains("Production", exception.Message, StringComparison.Ordinal);
    }
}
