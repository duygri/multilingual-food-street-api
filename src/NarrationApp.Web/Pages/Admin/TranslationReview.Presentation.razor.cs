using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Admin;

public partial class TranslationReview
{
    private static string GetMatrixLabel(ManagedLanguageDto language, TranslationDto? translation)
    {
        if (translation is null)
        {
            return "—";
        }

        if (language.Code == "vi")
        {
            return "Gốc";
        }

        return translation.WorkflowStatus switch
        {
            TranslationWorkflowStatus.AutoTranslated => "Auto",
            _ => "✓"
        };
    }

    private static string GetAudioStateValue(AudioDto? audio)
    {
        if (audio is null)
        {
            return "missing";
        }

        return audio.Status switch
        {
            AudioStatus.Ready => "ready",
            AudioStatus.Requested or AudioStatus.Generating => "generating",
            AudioStatus.Failed => "failed",
            _ => "missing"
        };
    }

    private static string GetAudioStateLabel(AudioDto? audio) => GetAudioStateValue(audio) switch
    {
        "ready" => "Audio sẵn",
        "generating" => "Đang tạo",
        "failed" => "Lỗi audio",
        _ => "Chưa có"
    };

    private static string GetAudioStateClass(AudioDto? audio) =>
        $"translation-review__audio-state--{GetAudioStateValue(audio)}";

    private string GetMatrixCellClass(AdminPoiDto poi, string languageCode) =>
        poi.Id == _selectedPoi?.Id && string.Equals(languageCode, _selectedLanguage, StringComparison.OrdinalIgnoreCase)
            ? "is-selected"
            : string.Empty;

    private static string GetMatrixBadgeClass(ManagedLanguageDto language, TranslationDto? translation)
    {
        if (translation is null)
        {
            return "translation-review__matrix-badge--empty";
        }

        if (string.Equals(language.Code, "vi", StringComparison.OrdinalIgnoreCase))
        {
            return "translation-review__matrix-badge--source";
        }

        return translation.WorkflowStatus switch
        {
            TranslationWorkflowStatus.AutoTranslated => "translation-review__matrix-badge--auto",
            _ => "translation-review__matrix-badge--reviewed"
        };
    }

    private string GetProgressWidth(IReadOnlyList<TranslationDto> translations)
    {
        if (_languages.Count == 0)
        {
            return "0%";
        }

        var ratio = (double)translations.Count / _languages.Count;
        return $"{Math.Round(ratio * 100d)}%";
    }

    private string GetProgressClass(IReadOnlyList<TranslationDto> translations)
    {
        if (_languages.Count == 0)
        {
            return "translation-review__progress--empty";
        }

        var ratio = (double)translations.Count / _languages.Count;
        if (ratio >= 0.99d)
        {
            return "translation-review__progress--complete";
        }

        return ratio >= 0.5d
            ? "translation-review__progress--medium"
            : "translation-review__progress--low";
    }
}
