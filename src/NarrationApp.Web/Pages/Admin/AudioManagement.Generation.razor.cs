using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class AudioManagement
{
    private void OpenGenerateModal(AdminPoiDto? poi = null)
    {
        var targetPoi = poi ?? _selectedPoi ?? FilteredPois.FirstOrDefault();
        if (targetPoi is null)
        {
            return;
        }

        _selectedPoi = targetPoi;
        _modalPoi = targetPoi;
        _selectedLanguages = new HashSet<string>(GetGenerateLanguageOptions(targetPoi).Select(item => item.Code), StringComparer.OrdinalIgnoreCase);
        _selectedVoiceProfile = "standard";
        _isGenerateModalOpen = true;
    }

    private void CloseGenerateModal()
    {
        _isGenerateModalOpen = false;
        _modalPoi = null;
        _isGenerating = false;
    }

    private void ToggleLanguage(string languageCode, object? value)
    {
        var isChecked = value switch
        {
            bool booleanValue => booleanValue,
            string stringValue => string.Equals(stringValue, "true", StringComparison.OrdinalIgnoreCase),
            _ => false
        };

        if (isChecked)
        {
            _selectedLanguages.Add(languageCode);
        }
        else
        {
            _selectedLanguages.Remove(languageCode);
        }
    }

    private async Task GenerateSelectedAudioAsync()
    {
        if (_modalPoi is null || _selectedLanguages.Count == 0)
        {
            return;
        }

        _isGenerating = true;

        try
        {
            var generatedCount = 0;
            foreach (var language in GetGenerateLanguageOptions(_modalPoi).Where(item => _selectedLanguages.Contains(item.Code)))
            {
                if (string.Equals(language.Code, "vi", StringComparison.OrdinalIgnoreCase))
                {
                    AppendAudio(await AudioPortalService.GenerateTtsAsync(new TtsGenerateRequest
                    {
                        PoiId = _modalPoi.Id,
                        LanguageCode = language.Code,
                        Script = _modalPoi.TtsScript,
                        VoiceProfile = _selectedVoiceProfile
                    }));
                    generatedCount++;
                    continue;
                }

                if (!GetTranslations(_modalPoi.Id).Any(item => item.LanguageCode == language.Code))
                {
                    continue;
                }

                AppendAudio(await AudioPortalService.GenerateFromTranslationAsync(new GenerateAudioFromTranslationRequest
                {
                    PoiId = _modalPoi.Id,
                    LanguageCode = language.Code,
                    VoiceProfile = _selectedVoiceProfile
                }));
                generatedCount++;
            }

            _statusMessage = $"Đã tạo {generatedCount} audio cho {_modalPoi.Name}.";
            CloseGenerateModal();
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isGenerating = false;
        }
    }

    private IReadOnlyList<AudioLanguageOption> GetGenerateLanguageOptions(AdminPoiDto poi) =>
        ActiveLanguageOptions
            .Where(language => CanGenerateLanguage(poi.Id, language.Code))
            .ToArray();

    private bool CanGenerateLanguage(int poiId, string languageCode)
    {
        if (HasCurrentAudioForLanguage(poiId, languageCode))
        {
            return false;
        }

        if (string.Equals(languageCode, "vi", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return GetTranslations(poiId).Any(item => string.Equals(item.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase));
    }

    private bool HasCurrentAudioForLanguage(int poiId, string languageCode) =>
        GetAudioItems(poiId).Any(item =>
            string.Equals(item.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase)
            && item.Status is not AudioStatus.Deleted and not AudioStatus.Replaced);
}
