using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.Category;

namespace NarrationApp.Server.Services;

public sealed class CategoryService(AppDbContext dbContext) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Categories
            .AsNoTracking()
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name);

        if (!includeInactive)
        {
            query = query.Where(item => item.IsActive).OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name);
        }

        var items = await query.ToListAsync(cancellationToken);
        return items.Select(item => item.ToDto()).ToArray();
    }

    public async Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Category
        {
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim().ToLowerInvariant(),
            Description = request.Description.Trim(),
            Icon = request.Icon.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Categories.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Categories.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Category was not found.");

        entity.Name = request.Name.Trim();
        entity.Slug = request.Slug.Trim().ToLowerInvariant();
        entity.Description = request.Description.Trim();
        entity.Icon = request.Icon.Trim();
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Categories.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Category was not found.");

        var hasPois = await dbContext.Pois.AnyAsync(item => item.CategoryId == id, cancellationToken);
        if (hasPois)
        {
            throw new InvalidOperationException("Category is still assigned to one or more POIs.");
        }

        dbContext.Categories.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
