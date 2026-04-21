using System.IO;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class TouristMapScriptTests
{
    [Fact]
    public void Tourist_map_script_does_not_attach_default_mapbox_navigation_controls()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "js", "touristMap.js");

        var script = File.ReadAllText(scriptPath);

        Assert.DoesNotContain("NavigationControl", script, StringComparison.Ordinal);
    }
}
