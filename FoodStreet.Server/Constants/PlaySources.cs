namespace FoodStreet.Server.Constants;

public static class PlaySources
{
    public const string Manual = "manual";
    public const string QrScan = "qr_scan";
    public const string Geofence = "geofence";
    public const string TourStart = "tour_start";
    public const string TourResume = "tour_resume";
    public const string TourProgress = "tour_progress";
    public const string TourDismiss = "tour_dismiss";
    public const string TourComplete = "tour_complete";

    public static string Normalize(string? source)
    {
        var normalized = source?.Trim().ToLowerInvariant();

        return normalized switch
        {
            null or "" => Manual,
            "manual" => Manual,
            "qr" or "qrcode" or "qr_scan" => QrScan,
            "geofence" => Geofence,
            "tour" or "tour_start" => TourStart,
            "tour_resume" or "resume" => TourResume,
            "tour_progress" or "tour_stop" => TourProgress,
            "tour_dismiss" or "dismiss" => TourDismiss,
            "tour_complete" or "tour_completed" or "complete" or "completed" => TourComplete,
            _ => normalized
        };
    }
}
