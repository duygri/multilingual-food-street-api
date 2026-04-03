namespace FoodStreet.Server.Constants;

public static class LocalizationCoverageMetrics
{
    public static readonly IReadOnlyList<string> SupportedLanguages =
    [
        "vi-VN",
        "en-US",
        "ja-JP",
        "ko-KR",
        "zh-CN"
    ];

    public static readonly IReadOnlyList<string> TranslationLanguages =
    [
        "en-US",
        "ja-JP",
        "ko-KR",
        "zh-CN"
    ];

    public static int TranslationSlotsPerItem => TranslationLanguages.Count;

    public static double CalculateCoveragePercent(int translationCount, int itemCount)
    {
        if (itemCount <= 0 || translationCount <= 0)
        {
            return 0;
        }

        var totalSlots = itemCount * TranslationSlotsPerItem;
        return Math.Round(Math.Min(100d, translationCount * 100d / totalSlots), 1);
    }

    public static int CountAvailableLanguages(IEnumerable<string?> languageCodes)
    {
        var available = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var code in languageCodes)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            if (TranslationLanguages.Contains(code, StringComparer.OrdinalIgnoreCase))
            {
                available.Add(code);
            }
        }

        return available.Count;
    }

    public static bool HasFullCoverage(IEnumerable<string?> languageCodes)
    {
        return CountAvailableLanguages(languageCodes) >= TranslationSlotsPerItem;
    }
}
