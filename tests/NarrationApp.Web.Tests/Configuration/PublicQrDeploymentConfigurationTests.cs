using Microsoft.Extensions.Configuration;

namespace NarrationApp.Web.Tests.Configuration;

public sealed class PublicQrDeploymentConfigurationTests
{
    [Fact]
    public void Web_development_config_points_public_qr_links_to_server_origin()
    {
        var webProjectPath = Path.Combine(GetRepositoryRoot(), "src", "NarrationApp.Web", "wwwroot");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(webProjectPath)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        Assert.Equal("https://localhost:5001/", configuration["QrPublicBaseUrl"]);
    }

    [Fact]
    public void Server_development_config_leaves_public_qr_base_url_empty_for_lan_fallback()
    {
        var serverProjectPath = Path.Combine(GetRepositoryRoot(), "src", "NarrationApp.Server");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(serverProjectPath)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        Assert.Equal(string.Empty, configuration["PublicQr:BaseUrl"]);
    }

    [Theory]
    [InlineData("appsettings.Staging.json")]
    [InlineData("appsettings.Production.json")]
    public void Server_deploy_environment_config_exposes_public_qr_base_url(string fileName)
    {
        var serverProjectPath = Path.Combine(GetRepositoryRoot(), "src", "NarrationApp.Server");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(serverProjectPath)
            .AddJsonFile(fileName, optional: false)
            .Build();

        Assert.NotNull(configuration["PublicQr:BaseUrl"]);
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}
