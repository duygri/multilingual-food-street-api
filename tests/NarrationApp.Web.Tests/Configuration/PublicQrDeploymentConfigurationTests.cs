using Microsoft.Extensions.Configuration;

namespace NarrationApp.Web.Tests.Configuration;

public sealed class PublicQrDeploymentConfigurationTests
{
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
