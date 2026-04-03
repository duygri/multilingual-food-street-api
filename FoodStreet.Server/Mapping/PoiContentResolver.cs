using FoodStreet.Server.Constants;
using PROJECT_C_.Models;

namespace FoodStreet.Server.Mapping;

public sealed record ResolvedPoiContent(
    string RequestedLanguageCode,
    string LanguageCode,
    int Tier,
    bool FallbackUsed,
    bool IsFallback,
    string Name,
    string Description,
    string? TtsScript,
    string? AudioUrl,
    string AudioStatus,
    bool HasAudio);

public static class PoiContentResolver
{
    public static string NormalizeLanguageCode(string? languageCode)
    {
        var candidate = languageCode?.Split(',')[0].Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return "vi-VN";
        }

        return candidate.ToLowerInvariant() switch
        {
            "vi" or "vi-vn" => "vi-VN",
            "en" or "en-us" => "en-US",
            "ja" or "ja-jp" => "ja-JP",
            "ko" or "ko-kr" => "ko-KR",
            "zh" or "zh-cn" => "zh-CN",
            _ => candidate
        };
    }

    public static ResolvedPoiContent Resolve(Location location, string? languageCode)
    {
        var requestedLanguage = NormalizeLanguageCode(languageCode);
        var translations = location.Translations ?? [];

        var exact = FindTranslation(translations, requestedLanguage);
        var english = requestedLanguage == "en-US" ? exact : FindTranslation(translations, "en-US");
        var vietnamese = FindTranslation(translations, "vi-VN");

        var resolved = exact ?? english ?? vietnamese;
        var tier = exact != null ? 1 : english != null ? 2 : 3;
        var fallbackUsed = tier > 1;

        var language = resolved?.LanguageCode ?? "vi-VN";
        var useBaseContent = resolved == null || string.Equals(language, "vi-VN", StringComparison.OrdinalIgnoreCase);
        var name = useBaseContent ? location.Name : resolved?.Name ?? location.Name;
        var description = useBaseContent ? location.Description : resolved?.Description ?? location.Description;
        var ttsScript = resolved?.TtsScript ?? location.TtsScript;

        var translationAudio = DecorateAudioUrl(resolved);
        var uploadedAudio = GetUploadedAudioStreamUrl(location);
        var audioUrl = translationAudio ?? uploadedAudio;
        var hasAudio = !string.IsNullOrWhiteSpace(audioUrl);
        var audioStatus = PoiAudioStatuses.Normalize(location.AudioStatus, hasAudio);

        return new ResolvedPoiContent(
            requestedLanguage,
            language,
            tier,
            fallbackUsed,
            resolved?.IsFallback ?? false,
            name,
            description,
            ttsScript,
            audioUrl,
            audioStatus,
            hasAudio);
    }

    private static LocationTranslation? FindTranslation(IEnumerable<LocationTranslation> translations, string languageCode)
    {
        return translations.FirstOrDefault(translation =>
            string.Equals(translation.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase));
    }

    private static string? DecorateAudioUrl(LocationTranslation? translation)
    {
        if (translation == null || string.IsNullOrWhiteSpace(translation.AudioUrl))
        {
            return null;
        }

        if (!translation.GeneratedAt.HasValue)
        {
            return translation.AudioUrl;
        }

        var separator = translation.AudioUrl.Contains('?') ? "&" : "?";
        var version = new DateTimeOffset(translation.GeneratedAt.Value, TimeSpan.Zero).ToUnixTimeSeconds();
        return $"{translation.AudioUrl}{separator}v={version}&l={translation.LanguageCode}";
    }

    private static string? GetUploadedAudioStreamUrl(Location location)
    {
        var audioFile = location.AudioFiles
            .OrderByDescending(file => file.UploadedAt)
            .FirstOrDefault();

        return audioFile == null ? null : $"/api/audio/{audioFile.Id}/stream";
    }
}
