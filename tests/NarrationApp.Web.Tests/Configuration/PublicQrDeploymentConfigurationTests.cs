using Microsoft.Extensions.Configuration;

namespace NarrationApp.Web.Tests.Configuration;

public sealed class PublicQrDeploymentConfigurationTests
{
    [Fact]
    public void Web_development_config_uses_safe_mapbox_placeholder()
    {
        var webProjectPath = Path.Combine(GetRepositoryRoot(), "src", "NarrationApp.Web", "wwwroot");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(webProjectPath)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        Assert.Equal("https://localhost:5001/", configuration["QrPublicBaseUrl"]);
        Assert.Equal("YOUR_MAPBOX_ACCESS_TOKEN_HERE", configuration["Mapbox:AccessToken"]);
        Assert.Equal("mapbox://styles/mapbox/streets-v12", configuration["Mapbox:StyleUrl"]);
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

    [Fact]
    public void Server_publish_script_can_embed_admin_mapbox_local_settings()
    {
        var scriptPath = Path.Combine(GetRepositoryRoot(), "scripts", "deploy_server.ps1");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("[string]$MapboxAccessToken", script, StringComparison.Ordinal);
        Assert.Contains("[string]$MapboxStyleUrl", script, StringComparison.Ordinal);
        Assert.Contains("$env:MAPBOX_ACCESS_TOKEN", script, StringComparison.Ordinal);
        Assert.Contains("wwwroot", script, StringComparison.Ordinal);
        Assert.Contains("appsettings.Local.json", script, StringComparison.Ordinal);
        Assert.Contains("Mapbox", script, StringComparison.Ordinal);
        Assert.Contains("AccessToken", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Server_install_script_can_write_admin_mapbox_local_settings()
    {
        var scriptPath = Path.Combine(GetRepositoryRoot(), "scripts", "install_and_run_server.ps1");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("[string]$MapboxAccessToken", script, StringComparison.Ordinal);
        Assert.Contains("[string]$MapboxStyleUrl", script, StringComparison.Ordinal);
        Assert.Contains("$env:MAPBOX_ACCESS_TOKEN", script, StringComparison.Ordinal);
        Assert.Contains("current", script, StringComparison.Ordinal);
        Assert.Contains("wwwroot", script, StringComparison.Ordinal);
        Assert.Contains("appsettings.Local.json", script, StringComparison.Ordinal);
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}
