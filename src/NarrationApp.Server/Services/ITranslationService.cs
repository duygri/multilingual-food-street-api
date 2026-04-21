using NarrationApp.Shared.DTOs.Translation;

namespace NarrationApp.Server.Services;

public interface ITranslationService
{
    Task<IReadOnlyList<TranslationDto>> GetByPoiAsync(int poiId, CancellationToken cancellationToken = default);

    Task<TranslationDto> UpsertAsync(CreateTranslationRequest request, CancellationToken cancellationToken = default);

    Task<TranslationDto> AutoTranslateAsync(int poiId, string targetLanguage, CancellationToken cancellationToken = default);

    Task DeleteAsync(int translationId, CancellationToken cancellationToken = default);
}
