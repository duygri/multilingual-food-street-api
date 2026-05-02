using NarrationApp.Shared.DTOs.Languages;

namespace NarrationApp.Web.Services;

public sealed class LanguagePortalService(ApiClient apiClient) : ILanguagePortalService
{
    public Task<IReadOnlyList<ManagedLanguageDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<IReadOnlyList<ManagedLanguageDto>>("api/languages", cancellationToken);
    }

    public Task<ManagedLanguageDto> CreateAsync(CreateManagedLanguageRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<CreateManagedLanguageRequest, ManagedLanguageDto>("api/admin/languages", request, cancellationToken);
    }

    public Task DeleteAsync(string code, CancellationToken cancellationToken = default)
    {
        var encodedCode = Uri.EscapeDataString(code);
        return apiClient.DeleteAsync($"api/admin/languages/{encodedCode}", cancellationToken);
    }
}
