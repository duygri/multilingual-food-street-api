using NarrationApp.Shared.DTOs.Category;

namespace NarrationApp.Web.Services;

public sealed class CategoryPortalService(ApiClient apiClient) : ICategoryPortalService
{
    public Task<IReadOnlyList<CategoryDto>> GetAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = includeInactive ? "api/categories?includeInactive=true" : "api/categories";
        return apiClient.GetAsync<IReadOnlyList<CategoryDto>>(query, cancellationToken);
    }

    public Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<SaveCategoryRequest, CategoryDto>("api/categories", request, cancellationToken);
    }

    public Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PutAsync<SaveCategoryRequest, CategoryDto>($"api/categories/{id}", request, cancellationToken);
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return apiClient.DeleteAsync($"api/categories/{id}", cancellationToken);
    }
}
