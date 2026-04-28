using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class CategoryManagement
{
    private bool _isLoading = true;
    private bool _isCreateMode;
    private bool _isEditorOpen;
    private string? _errorMessage;
    private string? _statusMessage;
    private IReadOnlyList<CategoryDto> _categories = Array.Empty<CategoryDto>();
    private CategoryDto? _selectedCategory;
    private CategoryEditorModel _editor = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _categories = await CategoryPortalService.GetAsync(includeInactive: true);
            if (_categories.Count == 0)
            {
                BeginCreate();
            }
        }
        catch (ApiException exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
