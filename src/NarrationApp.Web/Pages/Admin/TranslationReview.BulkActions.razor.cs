using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class TranslationReview
{
    private IEnumerable<ManagedLanguageDto> FilteredBulkTranslationLanguages
    {
        get
        {
            var query = _bulkLanguageSearch.Trim();
            var languages = BulkTranslationLanguages;

            if (!string.IsNullOrWhiteSpace(query))
            {
                languages = languages
                    .Where(language =>
                        language.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || language.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || language.NativeName.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            return languages
                .OrderByDescending(language => GetMissingTranslationCount(language.Code))
                .ThenBy(language => language.Code, StringComparer.OrdinalIgnoreCase);
        }
    }

    private void ToggleBulkTranslationPanel()
    {
        _isBulkTranslationPanelOpen = !_isBulkTranslationPanelOpen;
    }

    private int GetMissingTranslationCount(string languageCode)
    {
        var normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        return _pois.Count(poi => GetTranslations(poi.Id)
            .All(translation => !string.Equals(translation.LanguageCode, normalizedLanguageCode, StringComparison.OrdinalIgnoreCase)));
    }

    private string GetBulkLanguageSubtitle(ManagedLanguageDto language, int missingCount)
    {
        var nativeName = string.IsNullOrWhiteSpace(language.NativeName) || string.Equals(language.NativeName, language.DisplayName, StringComparison.OrdinalIgnoreCase)
            ? language.DisplayName
            : $"{language.DisplayName} · {language.NativeName}";

        return missingCount == 0
            ? $"{nativeName} · Đã đủ bản dịch"
            : $"{nativeName} · {missingCount} POI thiếu";
    }

    private async Task AutoTranslateLanguageAsync(string languageCode)
    {
        if (IsBulkTranslationRunning)
        {
            return;
        }

        var normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        var languageLabel = normalizedLanguageCode.ToUpperInvariant();
        var createdCount = 0;
        var failedCount = 0;

        _bulkTranslationLanguageInProgress = normalizedLanguageCode;
        _bulkTranslationStatusMessage = $"Đang dịch {languageLabel}...";
        await InvokeAsync(StateHasChanged);

        foreach (var poi in _pois)
        {
            var rowTranslations = GetTranslations(poi.Id);
            if (rowTranslations.Any(item => string.Equals(item.LanguageCode, normalizedLanguageCode, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            try
            {
                var generated = await TranslationPortalService.AutoTranslateAsync(poi.Id, normalizedLanguageCode);
                UpsertTranslationForPoi(poi.Id, generated);
                createdCount++;
            }
            catch (ApiException)
            {
                failedCount++;
            }
        }

        if (_selectedPoi is not null)
        {
            _translations = GetTranslations(_selectedPoi.Id);
            LoadEditorForLanguage(_selectedLanguage);
        }

        _bulkTranslationLanguageInProgress = null;
        _bulkTranslationStatusMessage = BuildBulkTranslationResultMessage(languageLabel, createdCount, failedCount);
        _statusMessage = _bulkTranslationStatusMessage;
    }

    private static string BuildBulkTranslationResultMessage(string languageLabel, int createdCount, int failedCount)
    {
        var message = createdCount > 0
            ? $"Đã tự động bổ sung {createdCount} bản dịch cho ngôn ngữ {languageLabel}."
            : $"Không có POI nào cần dịch cho ngôn ngữ {languageLabel}.";

        return failedCount > 0
            ? $"{message} {failedCount} POI chưa dịch được."
            : message;
    }
}
