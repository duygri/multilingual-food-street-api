using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class AudioManagement
{
    private async Task RetryFailedAudioAsync(AdminPoiDto poi)
    {
        var failedLanguageCodes = GetFailedLanguageCodes(GetAudioItems(poi.Id));
        if (failedLanguageCodes.Count == 0)
        {
            _statusMessage = $"Không có audio lỗi để retry cho {poi.Name}.";
            return;
        }

        try
        {
            var retriedCount = await RetryFailedLanguagesAsync(poi, failedLanguageCodes);
            _selectedPoi = poi;
            _statusMessage = retriedCount > 0
                ? $"Đã retry {retriedCount} audio lỗi cho {poi.Name}."
                : $"Không có audio lỗi có thể retry cho {poi.Name}.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }

    private async Task RetryAllFailedAudioAsync()
    {
        var backlogPois = FilteredPois
            .Select(poi => (Poi: poi, FailedLanguages: GetFailedLanguageCodes(GetAudioItems(poi.Id))))
            .Where(entry => entry.FailedLanguages.Count > 0)
            .ToArray();

        if (backlogPois.Length == 0)
        {
            _statusMessage = "Không có audio lỗi trong danh sách hiện tại.";
            return;
        }

        try
        {
            var retriedCount = 0;
            foreach (var entry in backlogPois)
            {
                retriedCount += await RetryFailedLanguagesAsync(entry.Poi, entry.FailedLanguages);
            }

            _statusMessage = retriedCount > 0
                ? $"Đã retry {retriedCount} audio lỗi trên {backlogPois.Length} POI."
                : "Không có audio lỗi có thể retry trong danh sách hiện tại.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }

    private async Task<int> RetryFailedLanguagesAsync(AdminPoiDto poi, IReadOnlyList<string> languageCodes)
    {
        var retriedCount = 0;
        foreach (var languageCode in languageCodes)
        {
            if (string.Equals(languageCode, "vi", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(poi.TtsScript))
                {
                    continue;
                }

                AppendAudio(await AudioPortalService.GenerateTtsAsync(new TtsGenerateRequest
                {
                    PoiId = poi.Id,
                    LanguageCode = "vi",
                    Script = poi.TtsScript,
                    VoiceProfile = "standard"
                }));
                retriedCount++;
                continue;
            }

            if (!GetTranslations(poi.Id).Any(item => string.Equals(item.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            AppendAudio(await AudioPortalService.GenerateFromTranslationAsync(new GenerateAudioFromTranslationRequest
            {
                PoiId = poi.Id,
                LanguageCode = languageCode,
                VoiceProfile = "standard"
            }));
            retriedCount++;
        }

        return retriedCount;
    }
}
