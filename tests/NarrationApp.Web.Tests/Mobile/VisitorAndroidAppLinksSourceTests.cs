using System.IO;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorAndroidAppLinksSourceTests
{
    [Fact]
    public void Android_main_activity_declares_verified_https_qr_hosts()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var mainActivityPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Platforms", "Android", "MainActivity.cs");

        var source = File.ReadAllText(mainActivityPath);

        Assert.Contains("DataSchemes = [\"https\"]", source, StringComparison.Ordinal);
        Assert.Contains("DataPathPrefixes = [\"/qr/\"]", source, StringComparison.Ordinal);
        Assert.Contains("AutoVerify = true", source, StringComparison.Ordinal);
        Assert.Contains("narration.app", source, StringComparison.Ordinal);
    }
}
