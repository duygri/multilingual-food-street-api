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
}
