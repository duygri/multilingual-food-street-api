using NarrationApp.Shared.DTOs.Audio;

namespace NarrationApp.Web.Services;

public sealed class AudioPortalService(ApiClient apiClient) : IAudioPortalService
{
    public Task<IReadOnlyList<AudioDto>> GetByPoiAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var query = $"api/audio?poiId={poiId}";
        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            query += $"&lang={Uri.EscapeDataString(languageCode)}";
        }

        return apiClient.GetAsync<IReadOnlyList<AudioDto>>(query, cancellationToken);
    }

    public Task<AudioDto> GenerateTtsAsync(TtsGenerateRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<TtsGenerateRequest, AudioDto>("api/audio/tts", request, cancellationToken);
    }

    public Task<AudioDto> GenerateFromTranslationAsync(GenerateAudioFromTranslationRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<GenerateAudioFromTranslationRequest, AudioDto>("api/audio/generate-from-translation", request, cancellationToken);
    }

    public Task<AudioDto> UploadAsync(UploadAudioRequest request, Stream content, CancellationToken cancellationToken = default)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(request.PoiId.ToString()), "poiId");
        form.Add(new StringContent(request.LanguageCode), "languageCode");

        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new("audio/mpeg");
        form.Add(fileContent, "file", request.FileName);

        return apiClient.PostMultipartAsync<AudioDto>("api/audio/upload", form, cancellationToken);
    }

    public Task<AudioDto> UpdateAsync(int audioId, UpdateAudioRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PutAsync<UpdateAudioRequest, AudioDto>($"api/audio/{audioId}", request, cancellationToken);
    }

    public Task DeleteAsync(int audioId, CancellationToken cancellationToken = default)
    {
        return apiClient.DeleteAsync($"api/audio/{audioId}", cancellationToken);
    }
}
