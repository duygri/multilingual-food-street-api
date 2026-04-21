namespace NarrationApp.Shared.DTOs.Category;

public sealed class CategoryDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Icon { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; }
}

public sealed class SaveCategoryRequest
{
    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Icon { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; } = true;
}
