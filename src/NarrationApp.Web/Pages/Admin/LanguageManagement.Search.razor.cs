using NarrationApp.Web.Features.Languages;

namespace NarrationApp.Web.Pages.Admin;

public partial class LanguageManagement
{
    private IReadOnlyList<LanguageCatalogEntry> _suggestions = Array.Empty<LanguageCatalogEntry>();

    private void UpdateSearchQuery(string? value)
    {
        _draft.SearchText = value?.TrimStart() ?? string.Empty;
        _suggestions = LanguageCatalog.Search(_draft.SearchText);
    }

    private void UpdateLanguageCode(string? value)
    {
        _draft.Code = value?.Trim().ToLowerInvariant() ?? string.Empty;
        var suggestion = LanguageCatalog.FindByCodeOrFlag(_draft.Code);
        if (suggestion is not null)
        {
            ApplySuggestion(suggestion.Code);
            _draft.SearchText = string.Empty;
        }
    }

    private void ApplySuggestion(string code)
    {
        var suggestion = LanguageCatalog.FindByCode(code);
        if (suggestion is null)
        {
            return;
        }

        _draft.SearchText = suggestion.DisplayName;
        _draft.Code = suggestion.Code;
        _draft.DisplayName = suggestion.DisplayName;
        _draft.NativeName = suggestion.NativeName;
        _draft.FlagCode = suggestion.FlagCode;
        _suggestions = Array.Empty<LanguageCatalogEntry>();
    }

    private void ResetDraft()
    {
        _draft = new LanguageDraft();
        _suggestions = Array.Empty<LanguageCatalogEntry>();
    }
}
