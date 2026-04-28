using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Owner;

public partial class PoiDetail
{
    private const long MaxAudioUploadBytes = 20_000_000;
    private const long MaxImageUploadBytes = 5_000_000;
    private readonly NarrationMode[] _narrationModes = Enum.GetValues<NarrationMode>();

    [Parameter]
    public int Id { get; set; }

    private bool _isLoading = true;
    private bool _isSaving;
    private bool _isSavingGeofence;
    private bool _isUploadingAudio;
    private bool _isUploadingImage;
    private bool _isRemovingImage;
    private bool _isRequestingReview;
    private bool _isDeleteDialogOpen;
    private string? _errorMessage;
    private string? _statusMessage;
    private PoiDto? _poi;
    private OwnerPoiDetailWorkspaceDto? _workspace;
    private PoiEditModel? _editor;
    private GeofenceEditModel? _geofenceEditor;
    private IBrowserFile? _selectedUploadFile;
    private IBrowserFile? _selectedImageFile;
    private IReadOnlyList<CategoryDto> _categories = Array.Empty<CategoryDto>();
    private IReadOnlyList<AudioDto> _audioItems = Array.Empty<AudioDto>();
    private IReadOnlyList<ModerationRequestDto> _moderationItems = Array.Empty<ModerationRequestDto>();

    protected override async Task OnParametersSetAsync()
    {
        ResetLoadState();

        try
        {
            _categories = await CategoryPortalService.GetAsync();
            _poi = await OwnerPortalService.GetPoiAsync(Id);
            _workspace = await OwnerPortalService.GetPoiWorkspaceAsync(Id);
            _audioItems = await AudioPortalService.GetByPoiAsync(Id);
            _moderationItems = await ModerationPortalService.GetMineAsync();
            HydrateEditors(_poi);
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

    private void ResetLoadState()
    {
        _isLoading = true;
        _errorMessage = null;
        _statusMessage = null;
        _poi = null;
        _workspace = null;
        _editor = null;
        _geofenceEditor = null;
        _selectedUploadFile = null;
        _selectedImageFile = null;
        _categories = Array.Empty<CategoryDto>();
        _audioItems = Array.Empty<AudioDto>();
        _moderationItems = Array.Empty<ModerationRequestDto>();
        _isDeleteDialogOpen = false;
    }

    private void HydrateEditors(PoiDto poi)
    {
        _editor = PoiEditModel.FromPoi(poi);
        _geofenceEditor = GeofenceEditModel.FromPoi(poi);
    }

    private async Task ReloadWorkspaceAsync()
    {
        _workspace = await OwnerPortalService.GetPoiWorkspaceAsync(Id);
    }
}
