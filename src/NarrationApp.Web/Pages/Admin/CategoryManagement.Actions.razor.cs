using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class CategoryManagement
{
    private async Task SaveAsync()
    {
        try
        {
            if (_isCreateMode)
            {
                var created = await CategoryPortalService.CreateAsync(_editor.ToRequest());
                _categories = _categories.Append(created).OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).ToArray();
                SelectCategory(created);
                _statusMessage = $"Đã tạo danh mục mới: {created.Name}.";
                return;
            }

            if (_selectedCategory is null) return;

            var updated = await CategoryPortalService.UpdateAsync(_selectedCategory.Id, _editor.ToRequest());
            _categories = _categories.Select(item => item.Id == updated.Id ? updated : item).OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).ToArray();
            SelectCategory(updated);
            _statusMessage = $"Đã cập nhật danh mục: {updated.Name}.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }

    private async Task DeleteAsync(CategoryDto category)
    {
        try
        {
            await CategoryPortalService.DeleteAsync(category.Id);
            _categories = _categories.Where(item => item.Id != category.Id).ToArray();

            if (_selectedCategory?.Id == category.Id)
            {
                _selectedCategory = null;
                _isEditorOpen = false;
            }

            _statusMessage = "Đã xóa danh mục.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }
}
