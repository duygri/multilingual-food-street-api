using System.IO;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class AndroidManifestTests
{
    [Fact]
    public void Mobile_manifest_allows_cleartext_traffic_for_local_dev_api()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var manifestPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Platforms", "Android", "AndroidManifest.xml");

        var manifest = File.ReadAllText(manifestPath);

        Assert.Contains("android:usesCleartextTraffic=\"true\"", manifest, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_manifest_declares_background_location_and_foreground_service_permissions()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var manifestPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Platforms", "Android", "AndroidManifest.xml");

        var manifest = File.ReadAllText(manifestPath);

        Assert.Contains("android.permission.ACCESS_BACKGROUND_LOCATION", manifest, StringComparison.Ordinal);
        Assert.Contains("android.permission.FOREGROUND_SERVICE", manifest, StringComparison.Ordinal);
        Assert.Contains("android.permission.FOREGROUND_SERVICE_LOCATION", manifest, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_manifest_registers_background_location_foreground_service()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var manifestPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Platforms", "Android", "AndroidManifest.xml");

        var manifest = File.ReadAllText(manifestPath);

        Assert.Contains("VisitorBackgroundLocationForegroundService", manifest, StringComparison.Ordinal);
        Assert.Contains("foregroundServiceType=\"location\"", manifest, StringComparison.Ordinal);
    }
}
