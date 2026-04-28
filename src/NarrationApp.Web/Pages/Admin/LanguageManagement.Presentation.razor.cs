using NarrationApp.Shared.DTOs.Languages;

namespace NarrationApp.Web.Pages.Admin;

public partial class LanguageManagement
{
    private static string FormatCoverage(ManagedLanguageDto language) => $"{Math.Round(GetCoverageRatio(language) * 100d)}%";

    private static double GetCoverageRatio(ManagedLanguageDto language)
    {
        if (language.TranslationCoverageTotal == 0) return 0d;
        return (double)language.TranslationCoverageCount / language.TranslationCoverageTotal;
    }

    private static string GetCoverageWidth(ManagedLanguageDto language) => $"{Math.Round(GetCoverageRatio(language) * 100d)}%";

    private static string GetCoverageTone(ManagedLanguageDto language)
    {
        var ratio = GetCoverageRatio(language);
        if (ratio >= 0.99d) return "language-admin__coverage-bar--complete";
        if (ratio >= 0.6d) return "language-admin__coverage-bar--medium";
        return "language-admin__coverage-bar--low";
    }
}
