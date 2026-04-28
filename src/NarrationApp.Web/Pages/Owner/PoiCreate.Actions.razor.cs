using Microsoft.AspNetCore.Components.Forms;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Owner;

public partial class PoiCreate
{
    private void HandleRepresentativeImageSelection(InputFileChangeEventArgs args)
    {
        _selectedImageFile = args.File;
        _statusMessage = null;
    }

    private Task SaveDraftAsync() => PersistPoiAsync(submitForReview: false);

    private Task SubmitReviewAsync() => PersistPoiAsync(submitForReview: true);

    private async Task PersistPoiAsync(bool submitForReview)
    {
        if (IsBusy)
        {
            return;
        }

        if (submitForReview)
        {
            _isSubmittingReview = true;
        }
        else
        {
            _isSavingDraft = true;
        }

        try
        {
            _statusMessage = null;

            var created = await OwnerPortalService.CreatePoiAsync(_editor.ToRequest(ParseCategoryId()));

            if (_selectedImageFile is not null)
            {
                created = await UploadRepresentativeImageAsync(created.Id);
            }

            NotifyOwnerPortalChanged();

            if (submitForReview)
            {
                await ModerationPortalService.CreateAsync(new CreateModerationRequest
                {
                    EntityType = "poi",
                    EntityId = created.Id.ToString()
                });

                NotifyOwnerPortalChanged();
                _statusMessage = "Đã gửi POI vào hàng chờ duyệt.";
                return;
            }

            NavigationManager.NavigateTo($"/owner/pois/{created.Id}");
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isSavingDraft = false;
            _isSubmittingReview = false;
        }
    }

    private async Task<PoiDto> UploadRepresentativeImageAsync(int poiId)
    {
        if (_selectedImageFile is null)
        {
            throw new InvalidOperationException("Representative image is not selected.");
        }

        await using var stream = _selectedImageFile.OpenReadStream(MaxRepresentativeImageBytes);
        return await OwnerPortalService.UploadPoiImageAsync(
            poiId,
            _selectedImageFile.Name,
            _selectedImageFile.ContentType,
            stream);
    }

    private void NotifyOwnerPortalChanged()
    {
        if (ServiceProvider.GetService(typeof(OwnerPortalRefreshService)) is OwnerPortalRefreshService refreshService)
        {
            refreshService.NotifyChanged();
        }
    }
}
