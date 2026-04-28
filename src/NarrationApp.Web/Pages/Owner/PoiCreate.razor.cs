using Microsoft.AspNetCore.Components.Forms;
using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Owner;

public partial class PoiCreate
{
    private const long MaxRepresentativeImageBytes = 5_000_000;
    private readonly NarrationMode[] _narrationModes = Enum.GetValues<NarrationMode>();

    private bool _isLoading = true;
    private bool _isSavingDraft;
    private bool _isSubmittingReview;
    private string? _errorMessage;
    private string? _statusMessage;
    private string _categoryId = string.Empty;
    private IReadOnlyList<CategoryDto> _categories = Array.Empty<CategoryDto>();
    private IBrowserFile? _selectedImageFile;
    private PoiCreateModel _editor = PoiCreateModel.CreateDefault();

    private bool IsBusy => _isSavingDraft || _isSubmittingReview;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _categories = await CategoryPortalService.GetAsync();
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
