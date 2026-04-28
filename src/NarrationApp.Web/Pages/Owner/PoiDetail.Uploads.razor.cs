using Microsoft.AspNetCore.Components.Forms;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Owner;

public partial class PoiDetail
{
    private void HandlePoiImageSelection(InputFileChangeEventArgs args)
    {
        _selectedImageFile = args.File;
        _statusMessage = null;
    }

    private async Task UploadPoiImageAsync()
    {
        if (_poi is null || _selectedImageFile is null)
        {
            return;
        }

        _isUploadingImage = true;

        try
        {
            await using var stream = _selectedImageFile.OpenReadStream(MaxImageUploadBytes);
            var updated = await OwnerPortalService.UploadPoiImageAsync(
                _poi.Id,
                _selectedImageFile.Name,
                _selectedImageFile.ContentType,
                stream);

            _poi = updated;
            HydrateEditors(updated);
            await ReloadWorkspaceAsync();
            _selectedImageFile = null;
            NotifyOwnerPortalChanged();
            _statusMessage = "Đã cập nhật ảnh đại diện POI.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isUploadingImage = false;
        }
    }

    private async Task RemovePoiImageAsync()
    {
        if (_poi is null)
        {
            return;
        }

        _isRemovingImage = true;

        try
        {
            var updated = await OwnerPortalService.DeletePoiImageAsync(_poi.Id);
            _poi = updated;
            HydrateEditors(updated);
            await ReloadWorkspaceAsync();
            NotifyOwnerPortalChanged();
            _statusMessage = "Đã gỡ ảnh đại diện POI.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isRemovingImage = false;
        }
    }

    private void HandleSourceAudioSelection(InputFileChangeEventArgs args)
    {
        _selectedUploadFile = args.File;
    }

    private async Task UploadSourceAudioAsync()
    {
        if (_poi is null || _selectedUploadFile is null)
        {
            return;
        }

        _isUploadingAudio = true;

        try
        {
            await using var stream = _selectedUploadFile.OpenReadStream(MaxAudioUploadBytes);
            await AudioPortalService.UploadAsync(new UploadAudioRequest
            {
                PoiId = _poi.Id,
                LanguageCode = "vi",
                FileName = _selectedUploadFile.Name
            }, stream);

            _audioItems = await AudioPortalService.GetByPoiAsync(_poi.Id);
            await ReloadWorkspaceAsync();
            _selectedUploadFile = null;
            NotifyOwnerPortalChanged();
            _statusMessage = "Đã tải audio nguồn tiếng Việt.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isUploadingAudio = false;
        }
    }
}
