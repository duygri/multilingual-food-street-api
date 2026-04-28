using System.IO;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorMapScriptTests
{
    [Fact]
    public void Visitor_map_script_does_not_attach_default_mapbox_navigation_controls()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "js", "visitorMap.js");

        var script = File.ReadAllText(scriptPath);

        Assert.DoesNotContain("NavigationControl", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Visitor_map_script_exposes_distinct_nearest_marker_state()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "js", "visitorMap.js");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("marker.isNearest ? \" is-nearest\" : \"\"", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Visitor_map_script_caches_center_and_marker_keys_to_avoid_full_redraws()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "js", "visitorMap.js");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("centerKey", script, StringComparison.Ordinal);
        Assert.Contains("markersKey", script, StringComparison.Ordinal);
        Assert.Contains("if (instance.markersKey === markersKey)", script, StringComparison.Ordinal);
    }
}
