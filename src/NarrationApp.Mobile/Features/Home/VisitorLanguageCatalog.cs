namespace NarrationApp.Mobile.Features.Home;

public static class VisitorLanguageCatalog
{
    private static readonly IReadOnlyDictionary<string, string> KnownSubLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "Tiếng Anh",
        ["ja"] = "Tiếng Nhật",
        ["ko"] = "Tiếng Hàn",
        ["zh"] = "Tiếng Trung",
        ["fr"] = "Tiếng Pháp",
        ["th"] = "Tiếng Thái",
        ["de"] = "Tiếng Đức",
        ["es"] = "Tiếng Tây Ban Nha",
        ["it"] = "Tiếng Ý",
        ["ru"] = "Tiếng Nga"
    };

    public static IReadOnlyList<VisitorLanguageOption> Defaults { get; } =
    [
        new("vi", "Tiếng Việt", "Mặc định", "VN"),
        new("en", "English", "Tiếng Anh", "GB"),
        new("ja", "日本語", "Tiếng Nhật", "JP"),
        new("ko", "한국어", "Tiếng Hàn", "KR"),
        new("zh", "中文", "Tiếng Trung", "CN"),
        new("fr", "Français", "Tiếng Pháp", "FR")
    ];

    public static VisitorLanguageOption CreateOption(
        string languageCode,
        string? displayName = null,
        string? nativeName = null,
        string? flagCode = null)
    {
        var code = NormalizeCode(languageCode);
        var display = displayName?.Trim() ?? string.Empty;
        var native = nativeName?.Trim() ?? string.Empty;
        var label = !string.IsNullOrWhiteSpace(native)
            ? native
            : !string.IsNullOrWhiteSpace(display)
                ? display
                : code.ToUpperInvariant();
        var subLabel = BuildSubLabel(code, label, display);
        var chipLabel = string.IsNullOrWhiteSpace(flagCode)
            ? code.ToUpperInvariant()
            : flagCode.Trim().ToUpperInvariant();

        return new VisitorLanguageOption(code, label, subLabel, chipLabel);
    }

    private static string NormalizeCode(string languageCode) =>
        string.IsNullOrWhiteSpace(languageCode)
            ? string.Empty
            : languageCode.Trim().ToLowerInvariant();

    private static string BuildSubLabel(string code, string label, string displayName)
    {
        if (string.Equals(code, "vi", StringComparison.OrdinalIgnoreCase))
        {
            return "Mặc định";
        }

        if (KnownSubLabels.TryGetValue(code, out var knownSubLabel))
        {
            return knownSubLabel;
        }

        if (!string.IsNullOrWhiteSpace(displayName)
            && !string.Equals(displayName, label, StringComparison.OrdinalIgnoreCase))
        {
            return displayName;
        }

        return string.IsNullOrWhiteSpace(displayName) ? code.ToUpperInvariant() : displayName;
    }
}
