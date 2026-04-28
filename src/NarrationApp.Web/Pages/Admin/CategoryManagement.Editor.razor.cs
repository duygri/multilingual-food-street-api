using NarrationApp.Shared.DTOs.Category;

namespace NarrationApp.Web.Pages.Admin;

public partial class CategoryManagement
{
    private void BeginCreate()
    {
        _isCreateMode = true;
        _isEditorOpen = true;
        _selectedCategory = null;
        _editor = new CategoryEditorModel();
        _statusMessage = null;
    }

    private void SelectCategory(CategoryDto category)
    {
        _isCreateMode = false;
        _isEditorOpen = true;
        _selectedCategory = category;
        _editor = CategoryEditorModel.FromDto(category);
        _statusMessage = null;
    }

    private void CloseEditor() => _isEditorOpen = false;
}
