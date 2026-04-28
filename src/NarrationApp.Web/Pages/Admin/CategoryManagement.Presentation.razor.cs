using NarrationApp.Shared.DTOs.Category;

namespace NarrationApp.Web.Pages.Admin;

public partial class CategoryManagement
{
    private static string GetIcon(CategoryDto category) => string.IsNullOrWhiteSpace(category.Icon) ? "•" : category.Icon;
}
