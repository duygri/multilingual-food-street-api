using NarrationApp.Shared.DTOs.Audio;

namespace NarrationApp.Web.Services;

public interface IAudioPortalService
{
    Task<IReadOnlyList<AudioDto>> GetByPoiAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default);

    Task<AudioDto> GenerateTtsAsync(TtsGenerateRequest request, CancellationToken cancellationToken = default);

    Task<AudioDto> GenerateFromTranslationAsync(GenerateAudioFromTranslationRequest request, CancellationToken cancellationToken = default);

    Task<AudioDto> UploadAsync(UploadAudioRequest request, Stream content, CancellationToken cancellationToken = default);

    Task<AudioDto> UpdateAsync(int audioId, UpdateAudioRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int audioId, CancellationToken cancellationToken = default);
}
