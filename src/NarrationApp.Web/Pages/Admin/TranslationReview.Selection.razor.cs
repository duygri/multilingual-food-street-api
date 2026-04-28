using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Translation;

namespace NarrationApp.Web.Pages.Admin;

public partial class TranslationReview
{
    private void ToggleReviewPanel()
    {
        if (_isReviewPanelOpen)
        {
            _isReviewPanelOpen = false;
            return;
        }

        if (_selectedPoi is null && _pois.Count > 0)
        {
            SelectPoiLanguage(_pois[0], _selectedLanguage, false);
        }

        _isReviewPanelOpen = true;
    }

    private void HandlePoiChanged(string? value)
    {
        if (!int.TryParse(value, out var poiId))
        {
            return;
        }

        var poi = _pois.FirstOrDefault(item => item.Id == poiId);
        if (poi is not null)
        {
            SelectPoiLanguage(poi, _selectedLanguage, false);
        }
    }

    private void SelectPoiLanguage(AdminPoiDto poi, string languageCode, bool openPanel = true)
    {
        _selectedPoi = poi;
        _selectedLanguage = languageCode;
        _translations = GetTranslations(poi.Id);
        LoadEditorForLanguage(languageCode);
        if (openPanel)
        {
            _isReviewPanelOpen = true;
        }
    }

    private Task HandleLanguageChangedAsync()
    {
        _selectedLanguage = _selectedLanguage.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(_selectedLanguage))
        {
            _selectedLanguage = "vi";
        }

        LoadEditorForLanguage(_selectedLanguage);
        return Task.CompletedTask;
    }

    private void LoadEditorForLanguage(string languageCode)
    {
        var existing = _translations.FirstOrDefault(item => string.Equals(item.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase));
        _selectedTranslation = existing;
        _editor = existing is null
            ? TranslationEditorModel.CreateDefault(languageCode)
            : TranslationEditorModel.FromDto(existing);
    }

    private void UpsertTranslation(TranslationDto translation)
    {
        if (_selectedPoi is null)
        {
            return;
        }

        UpsertTranslationForPoi(_selectedPoi.Id, translation);
        _translations = GetTranslations(_selectedPoi.Id);
        _selectedLanguage = translation.LanguageCode;
        LoadEditorForLanguage(translation.LanguageCode);
    }

    private void UpsertTranslationForPoi(int poiId, TranslationDto translation)
    {
        _translationsByPoi[poiId] = GetTranslations(poiId)
            .Where(item => !string.Equals(item.LanguageCode, translation.LanguageCode, StringComparison.OrdinalIgnoreCase))
            .Append(translation)
            .OrderBy(item => item.LanguageCode)
            .ToArray();
    }
}
