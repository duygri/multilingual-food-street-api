namespace NarrationApp.Server.Services;

public sealed class GoogleCloudTtsService(HttpClient httpClient, IGoogleAccessTokenProvider accessTokenProvider) : IGoogleTtsService
{
    private static readonly IReadOnlyDictionary<string, VoiceNameSet> VoiceNameMap = new Dictionary<string, VoiceNameSet>(StringComparer.OrdinalIgnoreCase)
    {
        ["vi-VN"] = new("vi-VN-Standard-A", "vi-VN-Wavenet-A", "vi-VN-Neural2-A"),
        ["en-US"] = new("en-US-Standard-A", "en-US-Wavenet-A", "en-US-Neural2-A"),
        ["ja-JP"] = new("ja-JP-Standard-A", "ja-JP-Wavenet-A", "ja-JP-Neural2-B"),
        ["ko-KR"] = new("ko-KR-Standard-A", "ko-KR-Wavenet-A", "ko-KR-Neural2-C"),
        ["fr-FR"] = new("fr-FR-Standard-F", "fr-FR-Wavenet-F", "fr-FR-Neural2-F"),
        ["cmn-CN"] = new("cmn-CN-Standard-A", "cmn-CN-Wavenet-A", null)
    };

    public string ProviderName => "google-cloud-tts";

    public async Task<byte[]> GenerateAsync(string script, string languageCode, string voiceProfile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            throw new ArgumentException("TTS script is required.", nameof(script));
        }

        var normalizedLanguageCode = NormalizeVoiceLanguageCode(languageCode);
        var normalizedVoiceProfile = NormalizeVoiceProfile(voiceProfile);
        var voiceName = ResolveVoiceName(normalizedLanguageCode, normalizedVoiceProfile);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://texttospeech.googleapis.com/v1/text:synthesize")
        {
            Content = JsonContent.Create(new
            {
                input = new
                {
                    text = script
                },
                voice = new
                {
                    languageCode = normalizedLanguageCode,
                    name = voiceName,
                    ssmlGender = voiceName is null ? "NEUTRAL" : null
                },
                audioConfig = new
                {
                    audioEncoding = "MP3"
                }
            })
        };

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await accessTokenProvider.GetAccessTokenAsync(cancellationToken));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "Google Cloud Text-to-Speech", cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<TtsResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Google Cloud TTS returned an empty response.");

        if (string.IsNullOrWhiteSpace(payload.AudioContent))
        {
            throw new InvalidOperationException("Google Cloud TTS did not return audio content.");
        }

        return Convert.FromBase64String(payload.AudioContent);
    }

    private static string NormalizeVoiceLanguageCode(string languageCode)
    {
        var normalized = languageCode.Trim();
        return normalized.ToLowerInvariant() switch
        {
            "vi" => "vi-VN",
            "en" => "en-US",
            "ko" => "ko-KR",
            "ja" => "ja-JP",
            "fr" => "fr-FR",
            "de" => "de-DE",
            "es" => "es-ES",
            "zh" => "cmn-CN",
            _ => normalized
        };
    }

    private static string NormalizeVoiceProfile(string voiceProfile)
    {
        return voiceProfile.Trim().ToLowerInvariant() switch
        {
            "wavenet" => "wavenet",
            "neural2" => "neural2",
            _ => "standard"
        };
    }

    private static string? ResolveVoiceName(string normalizedLanguageCode, string normalizedVoiceProfile)
    {
        if (!VoiceNameMap.TryGetValue(normalizedLanguageCode, out var set))
        {
            return null;
        }

        return normalizedVoiceProfile switch
        {
            "neural2" => set.Neural2 ?? set.WaveNet ?? set.Standard,
            "wavenet" => set.WaveNet ?? set.Standard,
            _ => set.Standard
        };
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string serviceName, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"{serviceName} request failed with {(int)response.StatusCode} {response.ReasonPhrase}: {body}",
            null,
            response.StatusCode);
    }

    private sealed class TtsResponse
    {
        public string AudioContent { get; init; } = string.Empty;
    }

    private sealed record VoiceNameSet(string? Standard, string? WaveNet, string? Neural2);
}
