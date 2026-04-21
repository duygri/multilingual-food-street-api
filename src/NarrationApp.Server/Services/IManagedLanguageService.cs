using NarrationApp.Shared.DTOs.Languages;

namespace NarrationApp.Server.Services;

public interface IManagedLanguageService
{
    Task<IReadOnlyList<ManagedLanguageDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<ManagedLanguageDto> CreateAsync(CreateManagedLanguageRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(string code, CancellationToken cancellationToken = default);
}
