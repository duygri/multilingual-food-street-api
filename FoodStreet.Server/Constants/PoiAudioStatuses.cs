namespace FoodStreet.Server.Constants;

public static class PoiAudioStatuses
{
    public const string Pending = "pending";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Ready = "ready";
    public const string Failed = "failed";

    public static string Normalize(string? status, bool hasAudio = false)
    {
        var normalized = status?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return hasAudio ? Ready : Pending;
        }

        return normalized switch
        {
            "completed" => Ready,
            "complete" => Ready,
            "ready" => Ready,
            "processing" => Running,
            "running" => Running,
            "queued" => Queued,
            "failed" => Failed,
            "pending" => Pending,
            _ => hasAudio ? Ready : Pending
        };
    }

    public static bool IsQueuedOrRunning(string? status)
    {
        var normalized = Normalize(status);
        return normalized == Queued || normalized == Running;
    }
}
