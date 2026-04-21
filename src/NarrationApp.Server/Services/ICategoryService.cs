using NarrationApp.Shared.DTOs.Category;

namespace NarrationApp.Server.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default);

    Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
