namespace NarrationApp.Web.Tests.Mobile;

public sealed class MobileProjectConfigurationTests
{
    [Fact]
    public void Mobile_project_defines_a_dedicated_smoke_configuration()
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
            "NarrationApp.Mobile.csproj");

        var projectFile = File.ReadAllText(Path.GetFullPath(filePath));

        Assert.Contains("<Configurations>Debug;Smoke;Staging;Release</Configurations>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<PropertyGroup Condition=\"'$(Configuration)' == 'Smoke'\">", projectFile, StringComparison.Ordinal);
        Assert.Contains("<ApplicationTitle>Food Street Tourist Smoke</ApplicationTitle>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<ApplicationId>com.foodstreet.tourist.smoke</ApplicationId>", projectFile, StringComparison.Ordinal);
    }

    [Fact]
    public void Web_test_project_pins_bunit_version_for_offline_restore_stability()
    {
        var filePath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "tests",
            "NarrationApp.Web.Tests",
            "NarrationApp.Web.Tests.csproj");

        var projectFile = File.ReadAllText(Path.GetFullPath(filePath));

        Assert.Contains("<PackageReference Include=\"bunit\" Version=\"1.40.0\" />", projectFile, StringComparison.Ordinal);
    }
}
