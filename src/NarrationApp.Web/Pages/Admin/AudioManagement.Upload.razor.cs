using Microsoft.AspNetCore.Components.Forms;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class AudioManagement
{
    private void OpenUploadModal(AdminPoiDto? poi = null)
    {
        var targetPoi = poi ?? _selectedPoi ?? FilteredPois.FirstOrDefault();
        if (targetPoi is null)
        {
            return;
        }

        _selectedPoi = targetPoi;
        _uploadPoi = targetPoi;
        _selectedUploadFile = null;
        _isUploadModalOpen = true;
    }

    private void CloseUploadModal()
    {
        _isUploadModalOpen = false;
        _uploadPoi = null;
        _selectedUploadFile = null;
        _isUploading = false;
    }

    private void HandleUploadSelection(InputFileChangeEventArgs args) => _selectedUploadFile = args.File;

    private async Task UploadSelectedAudioAsync()
    {
        if (_uploadPoi is null || _selectedUploadFile is null)
        {
            return;
        }

        _isUploading = true;

        try
        {
            await using var stream = _selectedUploadFile.OpenReadStream(MaxUploadBytes);
            AppendAudio(await AudioPortalService.UploadAsync(new UploadAudioRequest
            {
                PoiId = _uploadPoi.Id,
                LanguageCode = "vi",
                FileName = _selectedUploadFile.Name
            }, stream));

            _statusMessage = $"Đã tải audio nguồn tiếng Việt cho {_uploadPoi.Name}.";
            CloseUploadModal();
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isUploading = false;
        }
    }
}
