namespace NarrationApp.Mobile.Features.Home;

public sealed record VisitorBackgroundTrackingNotificationCopy(
    string Title,
    string Body,
    string ChannelName,
    string ChannelDescription);

public static class VisitorBrand
{
    public const string PreferredAppLanguageCodeKey = "visitor.preferences.app-language-code";

    public static string DisplayName(string? languageCode) =>
        UsesEnglish(languageCode) ? "Sai Gon Ke" : "Sài Gòn Kể";

    public static VisitorBackgroundTrackingNotificationCopy BackgroundTrackingNotification(
        string? languageCode,
        int intervalSeconds)
    {
        var safeIntervalSeconds = Math.Max(1, intervalSeconds);
        return UsesEnglish(languageCode)
            ? new VisitorBackgroundTrackingNotificationCopy(
                DisplayName("en"),
                $"Background location tracking is active • every {safeIntervalSeconds} seconds",
                "Background location tracking",
                "Keeps location active for nearby place narration while the app is in the background.")
            : new VisitorBackgroundTrackingNotificationCopy(
                DisplayName("vi"),
                $"Đang theo dõi vị trí trong nền • chu kỳ {safeIntervalSeconds} giây",
                "Theo dõi vị trí trong nền",
                "Duy trì vị trí để phát thuyết minh khi bạn đến gần một địa điểm.");
    }

    private static bool UsesEnglish(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return false;
        }

        var normalized = languageCode.Trim();
        var separatorIndex = normalized.IndexOfAny(['-', '_']);
        var primaryLanguage = separatorIndex > 0 ? normalized[..separatorIndex] : normalized;
        return string.Equals(primaryLanguage, "en", StringComparison.OrdinalIgnoreCase);
    }
}
