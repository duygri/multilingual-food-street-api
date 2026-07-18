using System.Security.Cryptography;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class MobileProjectConfigurationTests
{
    [Theory]
    [InlineData("Lora-OFL.txt", "Copyright 2011 The Lora Project Authors", "Reserved Font Name \"Lora\"", "1D9A970809AC804B582A6CE7F0EBC4E7FEFCBFD7FF6299CAD35EE656A21BE716")]
    [InlineData("BeVietnamPro-OFL.txt", "Copyright 2021 The Be Vietnam Pro Project Authors", "Reserved Font Name", "6B7F8F73609A25EA78C891E34CF37B06F8A676B7EA986E941E43B009110F2A85")]
    public void Mobile_project_bundles_exact_upstream_font_license_notices(
        string fileName,
        string copyrightNotice,
        string reservedFontNameNotice,
        string expectedSha256)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var fontsRoot = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "fonts");
        var licensePath = Path.Combine(fontsRoot, fileName);

        Assert.True(File.Exists(licensePath), $"Expected bundled license '{fileName}'.");
        var license = File.ReadAllText(licensePath);
        Assert.False(string.IsNullOrWhiteSpace(license));
        Assert.Contains("SIL OPEN FONT LICENSE Version 1.1", license, StringComparison.Ordinal);
        Assert.Contains(copyrightNotice, license, StringComparison.Ordinal);
        Assert.Contains(reservedFontNameNotice, license, StringComparison.Ordinal);
        Assert.Equal(expectedSha256, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(licensePath))));

        var readme = File.ReadAllText(Path.Combine(fontsRoot, "README.md"));
        Assert.Contains(fileName, readme, StringComparison.Ordinal);
        Assert.Contains("389b770410cc0b7c21c85673bfa2077420fe7f65", readme, StringComparison.Ordinal);

        var projectFile = File.ReadAllText(Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "NarrationApp.Mobile.csproj"));
        Assert.Contains("<MauiAsset Include=\"wwwroot\\fonts\\*-OFL.txt\"", projectFile, StringComparison.Ordinal);
        Assert.Contains("LogicalName=\"wwwroot/fonts/%(Filename)%(Extension)\"", projectFile, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_project_bundles_documented_vietnamese_font_assets()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var fontsRoot = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "fonts");
        var expectedFonts = new[]
        {
            "Lora-SemiBold.woff2",
            "BeVietnamPro-Regular.woff2",
            "BeVietnamPro-SemiBold.woff2"
        };

        foreach (var fontName in expectedFonts)
        {
            var fontPath = Path.Combine(fontsRoot, fontName);
            Assert.True(File.Exists(fontPath), $"Expected bundled font '{fontName}'.");
            Assert.True(new FileInfo(fontPath).Length > 0, $"Expected bundled font '{fontName}' to contain data.");
        }

        var readme = File.ReadAllText(Path.Combine(fontsRoot, "README.md"));
        Assert.Contains("OFL.txt", readme, StringComparison.Ordinal);
        Assert.Contains("SHA-256", readme, StringComparison.Ordinal);
        Assert.Contains("github.com/google/fonts/blob/", readme, StringComparison.Ordinal);
        Assert.Contains("ofl/lora", readme, StringComparison.Ordinal);
        Assert.Contains("ofl/bevietnampro", readme, StringComparison.Ordinal);
        foreach (var fontName in expectedFonts)
        {
            Assert.Contains(fontName, readme, StringComparison.Ordinal);
        }
    }

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
        Assert.Contains("<ApplicationTitle>Sai Gon Ke Smoke</ApplicationTitle>", projectFile, StringComparison.Ordinal);
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

        Assert.Contains("<ApplicationTitle>Sai Gon Ke</ApplicationTitle>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<ApplicationId>com.foodstreet.tourist</ApplicationId>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<ApplicationId>com.foodstreet.tourist.dev</ApplicationId>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<ApplicationId>com.foodstreet.tourist.smoke</ApplicationId>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<ApplicationId>com.foodstreet.tourist.staging</ApplicationId>", projectFile, StringComparison.Ordinal);
        Assert.DoesNotContain("<ApplicationId>com.foodstreet.visitor", projectFile, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_host_and_vector_assets_use_sai_gon_ke_brand()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var mobileRoot = Path.Combine(projectRoot, "src", "NarrationApp.Mobile");
        var projectFile = File.ReadAllText(Path.Combine(mobileRoot, "NarrationApp.Mobile.csproj"));
        var host = File.ReadAllText(Path.Combine(mobileRoot, "wwwroot", "index.html"));
        var iconBackground = File.ReadAllText(Path.Combine(mobileRoot, "Resources", "AppIcon", "appicon.svg"));
        var iconForeground = File.ReadAllText(Path.Combine(mobileRoot, "Resources", "AppIcon", "appiconfg.svg"));
        var splash = File.ReadAllText(Path.Combine(mobileRoot, "Resources", "Splash", "splash.svg"));

        Assert.Contains("<ApplicationTitle>Sai Gon Ke Dev</ApplicationTitle>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<ApplicationTitle>Sai Gon Ke Staging</ApplicationTitle>", projectFile, StringComparison.Ordinal);
        Assert.Contains("Color=\"#9A3412\"", projectFile, StringComparison.Ordinal);
        Assert.Contains("<title>Sài Gòn Kể</title>", host, StringComparison.Ordinal);
        Assert.Contains("name=\"theme-color\" content=\"#9A3412\"", host, StringComparison.Ordinal);
        Assert.Contains("#9A3412", iconBackground, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aria-label=\"Sài Gòn Kể\"", iconForeground, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Sài Gòn Kể\"", splash, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_native_window_and_background_notification_use_shared_localized_brand_copy()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var mobileRoot = Path.Combine(projectRoot, "src", "NarrationApp.Mobile");
        var appSource = File.ReadAllText(Path.Combine(mobileRoot, "App.xaml.cs"));
        var startupSource = File.ReadAllText(Path.Combine(mobileRoot, "Components", "Pages", "Home.Startup.razor.cs"));
        var notificationSource = File.ReadAllText(Path.Combine(mobileRoot, "Platforms", "Android", "Services", "VisitorBackgroundLocationForegroundService.cs"));

        Assert.Contains("VisitorBrand.DisplayName", appSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Title = \"NarrationApp.Mobile\"", appSource, StringComparison.Ordinal);
        Assert.Contains("VisitorBrand.PreferredAppLanguageCodeKey", startupSource, StringComparison.Ordinal);
        Assert.DoesNotContain("private const string PreferredAppLanguageCodeKey", startupSource, StringComparison.Ordinal);
        Assert.Contains("Preferences.Default.Get(VisitorBrand.PreferredAppLanguageCodeKey", notificationSource, StringComparison.Ordinal);
        Assert.Contains("VisitorBrand.BackgroundTrackingNotification", notificationSource, StringComparison.Ordinal);
        Assert.Contains(".SetContentTitle(copy.Title)", notificationSource, StringComparison.Ordinal);
        Assert.Contains(".SetContentText(copy.Body)", notificationSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Food Street Visitor", notificationSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Background tracking đang chạy", notificationSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_user_facing_title_contexts_reject_legacy_brand_literals()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var mobileRoot = Path.Combine(projectRoot, "src", "NarrationApp.Mobile");
        var userFacingSources = new[]
        {
            Path.Combine(mobileRoot, "App.xaml.cs"),
            Path.Combine(mobileRoot, "Components", "Pages", "Home.razor"),
            Path.Combine(mobileRoot, "Components", "Pages", "Sections", "VisitorAboutScreen.razor"),
            Path.Combine(mobileRoot, "Platforms", "Android", "Services", "VisitorBackgroundLocationForegroundService.cs"),
            Path.Combine(mobileRoot, "wwwroot", "index.html"),
            Path.Combine(mobileRoot, "NarrationApp.Mobile.csproj")
        };

        foreach (var sourcePath in userFacingSources)
        {
            var source = File.ReadAllText(sourcePath);
            Assert.DoesNotContain("Food Street Visitor", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(">Food Street<", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Title = \"NarrationApp.Mobile\"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("NarrationApp Mobile", source, StringComparison.Ordinal);
        }
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
    public void Mobile_project_pins_sqlite_android_native_package_with_sixteen_kb_page_size_support()
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

        Assert.Contains("<PackageReference Include=\"SQLitePCLRaw.bundle_green\" Version=\"2.1.11\" />", projectFile, StringComparison.Ordinal);
        Assert.Contains("<PackageReference Include=\"SQLitePCLRaw.lib.e_sqlite3.android\" Version=\"2.1.11\" />", projectFile, StringComparison.Ordinal);
        Assert.DoesNotContain("SQLitePCLRaw.lib.e_sqlite3.android\" Version=\"2.1.2\"", projectFile, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_project_removes_default_visitor_api_asset_when_custom_config_is_provided()
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

        Assert.Contains(
            "<MauiAsset Remove=\"Resources\\Raw\\visitor-api.json\" Condition=\"'$(VisitorApiConfigFile)' != ''\" />",
            projectFile,
            StringComparison.Ordinal);
        Assert.Contains(
            "<Content Remove=\"Resources\\Raw\\visitor-api.json\" Condition=\"'$(VisitorApiConfigFile)' != ''\" />",
            projectFile,
            StringComparison.Ordinal);
        Assert.Contains(
            "<MauiAsset Include=\"$(VisitorApiConfigFile)\" Condition=\"'$(VisitorApiConfigFile)' != '' and Exists('$(VisitorApiConfigFile)')\" LogicalName=\"visitor-api.json\" />",
            projectFile,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_default_visitor_api_config_does_not_package_stale_physical_device_lan_ip()
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
            "Resources",
            "Raw",
            "visitor-api.json");

        var apiConfig = File.ReadAllText(Path.GetFullPath(filePath));

        Assert.DoesNotContain("192.168.98.219", apiConfig, StringComparison.Ordinal);
        Assert.DoesNotContain("192.168.31.137", apiConfig, StringComparison.Ordinal);
        Assert.Contains("\"androidDevice\": \"http://192.168.31.136:5000/\"", apiConfig, StringComparison.Ordinal);
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
