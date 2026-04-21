using NarrationApp.Shared.DTOs.Translation;

namespace NarrationApp.Web.Services;

public interface ITranslationPortalService
{
    Task<IReadOnlyList<TranslationDto>> GetByPoiAsync(int poiId, CancellationToken cancellationToken = default);

    Task<TranslationDto> SaveAsync(CreateTranslationRequest request, CancellationToken cancellationToken = default);

    Task<TranslationDto> AutoTranslateAsync(int poiId, string targetLanguage, CancellationToken cancellationToken = default);

    Task DeleteAsync(int translationId, CancellationToken cancellationToken = default);
}
