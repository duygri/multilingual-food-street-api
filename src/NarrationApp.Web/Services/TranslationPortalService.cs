using NarrationApp.Shared.DTOs.Translation;

namespace NarrationApp.Web.Services;

public sealed class TranslationPortalService(ApiClient apiClient) : ITranslationPortalService
{
    public Task<IReadOnlyList<TranslationDto>> GetByPoiAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<TranslationDto>>($"api/translations?poiId={poiId}", cancellationToken);
    }

    public Task<TranslationDto> SaveAsync(CreateTranslationRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<CreateTranslationRequest, TranslationDto>("api/translations", request, cancellationToken);
    }

    public Task<TranslationDto> AutoTranslateAsync(int poiId, string targetLanguage, CancellationToken cancellationToken = default)
    {
        var encodedLanguage = Uri.EscapeDataString(targetLanguage);
        return apiClient.PostAsync<object, TranslationDto>($"api/translations/{poiId}/auto?targetLanguage={encodedLanguage}", new { }, cancellationToken);
    }

    public Task DeleteAsync(int translationId, CancellationToken cancellationToken = default)
    {
        return apiClient.DeleteAsync($"api/translations/{translationId}", cancellationToken);
    }
}
