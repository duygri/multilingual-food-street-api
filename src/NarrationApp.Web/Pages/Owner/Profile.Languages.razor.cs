using NarrationApp.Shared.DTOs.Languages;

namespace NarrationApp.Web.Pages.Owner;

public partial class Profile
{
    private IReadOnlyList<PreferredLanguageOption> PreferredLanguageOptions { get; set; } = [];

    private static IReadOnlyList<PreferredLanguageOption> BuildPreferredLanguageOptions(
        IReadOnlyList<ManagedLanguageDto> languages,
        string? selectedLanguage)
    {
        var activeLanguages = languages
            .Where(item => item.IsActive)
            .OrderBy(item => item.Role)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(item => new PreferredLanguageOption(item.Code, item.NativeName))
            .ToList();

        if (!string.IsNullOrWhiteSpace(selectedLanguage)
            && activeLanguages.All(item => !string.Equals(item.Code, selectedLanguage, StringComparison.OrdinalIgnoreCase)))
        {
            activeLanguages.Insert(0, new PreferredLanguageOption(selectedLanguage.Trim(), selectedLanguage.Trim()));
        }

        return activeLanguages;
    }

    private sealed record PreferredLanguageOption(string Code, string NativeName);
}
