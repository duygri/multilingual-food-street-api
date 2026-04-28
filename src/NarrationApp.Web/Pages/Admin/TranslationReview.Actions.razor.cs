using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class TranslationReview
{
    private async Task AutoTranslateAsync()
    {
        if (_selectedPoi is null)
        {
            return;
        }

        try
        {
            var generated = await TranslationPortalService.AutoTranslateAsync(_selectedPoi.Id, _selectedLanguage);
            UpsertTranslation(generated);
            await RefreshAudioForPoiAsync(_selectedPoi.Id);
            _statusMessage = $"Đã tạo bản dịch nháp cho ngôn ngữ {_selectedLanguage}.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }

    private async Task AutoTranslateAllAsync()
    {
        var createdCount = 0;
        HashSet<int> touchedPoiIds = [];

        foreach (var poi in _pois)
        {
            var rowTranslations = GetTranslations(poi.Id);
            foreach (var language in _languages.Where(item => item.Code != "vi"))
            {
                if (rowTranslations.Any(item => item.LanguageCode == language.Code))
                {
                    continue;
                }

                try
                {
                    var generated = await TranslationPortalService.AutoTranslateAsync(poi.Id, language.Code);
                    UpsertTranslationForPoi(poi.Id, generated);
                    createdCount++;
                    touchedPoiIds.Add(poi.Id);
                }
                catch (ApiException)
                {
                }
            }
        }

        await RefreshAudioForPoisAsync(touchedPoiIds);

        if (_selectedPoi is not null)
        {
            _translations = GetTranslations(_selectedPoi.Id);
            LoadEditorForLanguage(_selectedLanguage);
        }

        _statusMessage = $"Đã tự động bổ sung {createdCount} bản dịch.";
    }

    private async Task SaveTranslationAsync()
    {
        if (_selectedPoi is null)
        {
            return;
        }

        try
        {
            var saved = await TranslationPortalService.SaveAsync(_editor.ToRequest(_selectedPoi.Id));
            UpsertTranslation(saved);
            await RefreshAudioForPoiAsync(_selectedPoi.Id);
            _statusMessage = $"Đã lưu bản dịch {_selectedLanguage}.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }

    private async Task DeleteTranslationAsync()
    {
        if (_selectedTranslation is null || _selectedPoi is null)
        {
            return;
        }

        try
        {
            await TranslationPortalService.DeleteAsync(_selectedTranslation.Id);
            _translationsByPoi[_selectedPoi.Id] = _translations.Where(item => item.Id != _selectedTranslation.Id).ToArray();
            _translations = GetTranslations(_selectedPoi.Id);
            _selectedTranslation = null;
            _editor = TranslationEditorModel.CreateDefault(_selectedLanguage);
            _statusMessage = $"Đã xóa bản dịch {_selectedLanguage}.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }
}
