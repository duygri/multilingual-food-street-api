using System.IO;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class RoutesMarkupTests
{
    [Fact]
    public void Mobile_routes_do_not_use_unsupported_router_not_found_page_parameter()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var routesPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Routes.razor");

        var markup = File.ReadAllText(routesPath);

        Assert.DoesNotContain("NotFoundPage=", markup, StringComparison.Ordinal);
    }
}
