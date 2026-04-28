using System.IO;

namespace NarrationApp.Web.Tests.Pages.Public;

public sealed class QrLaunchPageSourceTests
{
    [Fact]
    public void Public_qr_page_exists_and_exposes_qr_route_with_app_handoff()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pagePath = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Public", "QrLaunch.razor");
        var codeBehindPath = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Public", "QrLaunch.razor.cs");

        Assert.True(File.Exists(pagePath), "Expected a public QR landing page at Pages/Public/QrLaunch.razor.");
        Assert.True(File.Exists(codeBehindPath), "Expected QrLaunch.razor.cs next to the public QR page.");

        var source = File.ReadAllText(pagePath);

        Assert.Contains("@page \"/qr/{Code}\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@code", source, StringComparison.Ordinal);
        var codeBehind = File.ReadAllText(codeBehindPath);
        Assert.Contains("partial class QrLaunch", codeBehind, StringComparison.Ordinal);
        Assert.Contains("foodstreet://qr/", codeBehind, StringComparison.Ordinal);
    }
}
