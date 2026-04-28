namespace NarrationApp.Mobile.Features.Home;

public static class VisitorProfilePresentationFormatter
{
    public static string GetDefaultProfileName() => "Khách tham quan";

    public static string GetDisplayName(string? draftName) =>
        string.IsNullOrWhiteSpace(draftName) ? GetDefaultProfileName() : draftName;

    public static string GetDisplayEmail(string? draftEmail) =>
        string.IsNullOrWhiteSpace(draftEmail) ? "Chưa đặt email liên hệ" : draftEmail;

    public static string GetModeLabel() =>
        "Cục bộ trên thiết bị • không cần đăng nhập";

    public static string GetInitials(string? displayName)
    {
        var source = GetDisplayName(displayName);
        var parts = source.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "GT";
        }

        if (parts.Length == 1)
        {
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        }

        return string.Concat(parts[0][0], parts[^1][0]).ToUpperInvariant();
    }

    public static string NormalizeDraftName(string? draftName) =>
        string.IsNullOrWhiteSpace(draftName) ? GetDefaultProfileName() : draftName.Trim();

    public static string NormalizeDraftEmail(string? draftEmail) =>
        string.IsNullOrWhiteSpace(draftEmail) ? string.Empty : draftEmail.Trim();
}
