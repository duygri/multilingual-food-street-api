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
        Assert.Contains("<ApplicationTitle>Food Street Visitor Smoke</ApplicationTitle>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<ApplicationId>com.foodstreet.tourist.smoke</ApplicationId>", projectFile, StringComparison.Ordinal);
        Assert.DoesNotContain("<ApplicationId>com.foodstreet.visitor.smoke</ApplicationId>", projectFile, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_application_id_keeps_legacy_android_package_identity()
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

        Assert.Contains("<ApplicationTitle>Food Street Visitor</ApplicationTitle>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<ApplicationId>com.foodstreet.tourist</ApplicationId>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<ApplicationId>com.foodstreet.tourist.dev</ApplicationId>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<ApplicationId>com.foodstreet.tourist.smoke</ApplicationId>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<ApplicationId>com.foodstreet.tourist.staging</ApplicationId>", projectFile, StringComparison.Ordinal);
        Assert.DoesNotContain("<ApplicationId>com.foodstreet.visitor", projectFile, StringComparison.Ordinal);
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

    [Fact]
    public void Mobile_legacy_auth_runtime_files_have_been_removed()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        Assert.False(File.Exists(Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Features", "Home", "VisitorAuthApiService.cs")));
        Assert.False(File.Exists(Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Features", "Home", "VisitorAuthSession.cs")));
        Assert.False(File.Exists(Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Features", "Home", "VisitorTourSessionApiService.cs")));
        Assert.False(File.Exists(Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Services", "SecureVisitorAuthSessionStore.cs")));
    }

    [Fact]
    public void Mobile_domain_naming_uses_visitor_terms()
    {
        var mobileRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "NarrationApp.Mobile"));
        var oldPascalToken = "Tour" + "ist";
        var oldLowerToken = "tour" + "ist";

        var staleMatches = Directory
            .EnumerateFiles(mobileRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path =>
            {
                var relativePath = Path.GetRelativePath(mobileRoot, path);
                var content = string.Join(
                    Environment.NewLine,
                    File.ReadLines(path).Where(line => !line.Contains("<ApplicationId>", StringComparison.Ordinal)));

                return new[]
                {
                    relativePath.Contains(oldPascalToken, StringComparison.Ordinal) ||
                    relativePath.Contains(oldLowerToken, StringComparison.Ordinal)
                        ? relativePath
                        : null,
                    content.Contains(oldPascalToken, StringComparison.Ordinal) ||
                    content.Contains(oldLowerToken, StringComparison.Ordinal)
                        ? relativePath
                        : null
                };
            })
            .Where(match => match is not null)
            .Distinct()
            .ToArray();

        Assert.Empty(staleMatches);
    }
}
