using NarrationApp.Shared.DTOs.Category;

namespace NarrationApp.Web.Pages.Admin;

public partial class CategoryManagement
{
    private sealed class CategoryEditorModel
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;

        public static CategoryEditorModel FromDto(CategoryDto dto) => new()
        {
            Name = dto.Name,
            Slug = dto.Slug,
            Description = dto.Description,
            Icon = dto.Icon,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive
        };

        public SaveCategoryRequest ToRequest() => new()
        {
            Name = Name.Trim(),
            Slug = Slug.Trim(),
            Description = Description.Trim(),
            Icon = Icon.Trim(),
            DisplayOrder = DisplayOrder,
            IsActive = IsActive
        };
    }
}
