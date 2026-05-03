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
    public void Visitor_map_script_renders_a_distinct_user_location_marker_when_available()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "js", "visitorMap.js");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("snapshot.userLocation", script, StringComparison.Ordinal);
        Assert.Contains("visitor-map-user-marker", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Visitor_map_script_updates_user_location_without_recreating_poi_markers()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "js", "visitorMap.js");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("userLocationKey", script, StringComparison.Ordinal);
        Assert.Contains("updateUserMarker(instance, snapshot.userLocation)", script, StringComparison.Ordinal);
        Assert.Contains("setLngLat([userLocation.longitude, userLocation.latitude])", script, StringComparison.Ordinal);
        Assert.Contains("if (markersChanged)", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Visitor_map_script_schedules_map_resize_instead_of_resizing_on_every_render()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "js", "visitorMap.js");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("queueResize(instance)", script, StringComparison.Ordinal);
        Assert.Contains("requestAnimationFrame", script, StringComparison.Ordinal);
        Assert.Contains("resizeQueued", script, StringComparison.Ordinal);
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

    [Fact]
    public void Visitor_map_script_fits_bounds_when_multiple_markers_are_visible()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "js", "visitorMap.js");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("const hasMultipleMarkers = snapshot.markers.length > 1;", script, StringComparison.Ordinal);
        Assert.Contains("instance.map.fitBounds(bounds", script, StringComparison.Ordinal);
        Assert.Contains("padding: { top: 164, right: 28, bottom: 220, left: 28 }", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Visitor_map_script_renders_clickable_offline_fallback_when_mapbox_is_unavailable()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "js", "visitorMap.js");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("renderOfflineFallback(container, snapshot, dotNetRef", script, StringComparison.Ordinal);
        Assert.Contains("visitor-map-offline", script, StringComparison.Ordinal);
        Assert.Contains("createOfflineMarkerButton", script, StringComparison.Ordinal);
        Assert.Contains("dotNetRef.invokeMethodAsync(\"SelectPoiFromMap\", marker.id)", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Visitor_map_script_projects_offline_markers_from_snapshot_coordinates()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "js", "visitorMap.js");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("projectOfflinePoint(marker.latitude, marker.longitude, bounds)", script, StringComparison.Ordinal);
        Assert.Contains("calculateOfflineBounds(snapshot)", script, StringComparison.Ordinal);
        Assert.Contains("snapshot.userLocation", script, StringComparison.Ordinal);
    }
}
