using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public interface IAudioService
{
    Task<IReadOnlyList<AudioDto>> GetByPoiAsync(int poiId, string? languageCode, CancellationToken cancellationToken = default);

    Task<AudioDto?> GetByIdAsync(int audioId, CancellationToken cancellationToken = default);

    Task<AudioDto> UploadAsync(Guid actorUserId, UserRole actorRole, UploadAudioRequest request, Stream content, CancellationToken cancellationToken = default);

    Task<AudioDto> GenerateTtsAsync(Guid actorUserId, UserRole actorRole, TtsGenerateRequest request, CancellationToken cancellationToken = default);

    Task<AudioDto> GenerateFromTranslationAsync(Guid actorUserId, UserRole actorRole, GenerateAudioFromTranslationRequest request, CancellationToken cancellationToken = default);

    Task<AudioDto> UpdateAsync(Guid actorUserId, UserRole actorRole, int audioId, UpdateAudioRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid actorUserId, UserRole actorRole, int audioId, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadStreamAsync(int audioId, CancellationToken cancellationToken = default);
}
