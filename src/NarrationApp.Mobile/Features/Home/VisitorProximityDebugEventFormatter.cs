namespace NarrationApp.Mobile.Features.Home;

public static class VisitorProximityDebugEventFormatter
{
    public static string BuildActive(VisitorProximityMatch match) =>
        $"Active • {match.PoiName} • {match.DistanceMeters}m • ưu tiên {match.Priority}";

    public static string BuildQueued(VisitorProximityMatch match) =>
        $"Queued • {match.PoiName} • {match.DistanceMeters}m • ưu tiên {match.Priority}";

    public static string BuildPromoted(VisitorProximityMatch match) =>
        $"Promoted • {match.PoiName} • {match.DistanceMeters}m • ưu tiên {match.Priority}";

    public static string BuildExited(VisitorProximityMatch match) =>
        $"Exited • {match.PoiName}";
}
