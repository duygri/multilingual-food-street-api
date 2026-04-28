namespace NarrationApp.Web.Tests.Mobile;

public sealed class MauiProgramConfigurationTests
{
    [Fact]
    public void MauiProgram_MapsSmokeBuildsToDevelopmentApiEnvironment()
    {
        var filePath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "NarrationApp.Mobile",
            "MauiProgram.cs");

        var source = File.ReadAllText(Path.GetFullPath(filePath));

        Assert.Contains("#elif DEBUG || SMOKE", source, StringComparison.Ordinal);
        Assert.Contains("return VisitorApiDeploymentEnvironment.Development;", source, StringComparison.Ordinal);
    }
}
