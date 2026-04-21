using NarrationApp.Shared.DTOs.Category;

namespace NarrationApp.Web.Services;

public interface ICategoryPortalService
{
    Task<IReadOnlyList<CategoryDto>> GetAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default);

    Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
