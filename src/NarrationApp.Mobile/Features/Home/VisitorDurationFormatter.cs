namespace NarrationApp.Mobile.Features.Home;

public static class VisitorDurationFormatter
{
    public static string FormatSeconds(int totalSeconds)
    {
        var timeSpan = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
        return $"{(int)timeSpan.TotalMinutes}:{timeSpan.Seconds:00}";
    }
}
