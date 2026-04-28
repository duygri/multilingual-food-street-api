using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Owner;

public partial class PoiDetail
{
    private IReadOnlyList<ModerationRequestDto> PoiModerationItems => _moderationItems
        .Where(item => string.Equals(item.EntityType, "poi", StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.EntityId, Id.ToString(), StringComparison.Ordinal))
        .OrderByDescending(item => item.CreatedAtUtc)
        .ToArray();

    private ModerationRequestDto? LatestRejectedModeration => PoiModerationItems
        .FirstOrDefault() is { Status: ModerationStatus.Rejected } latestRejected ? latestRejected : null;

    private bool ShouldShowRejectionSurface => _poi?.Status == PoiStatus.Rejected || LatestRejectedModeration is not null;

    private string RejectionNote => LatestRejectedModeration?.ReviewNote
        ?? "POI đang bị từ chối. Kiểm tra nội dung nguồn, ảnh đại diện, geofence và audio rồi gửi lại để admin duyệt.";

    private async Task RequestReviewAsync()
    {
        if (_poi is null)
        {
            return;
        }

        _isRequestingReview = true;

        try
        {
            await ModerationPortalService.CreateAsync(new CreateModerationRequest
            {
                EntityType = "poi",
                EntityId = _poi.Id.ToString()
            });

            _moderationItems = await ModerationPortalService.GetMineAsync();
            NotifyOwnerPortalChanged();
            _statusMessage = $"Đã gửi {_poi.Name} vào hàng chờ duyệt.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isRequestingReview = false;
        }
    }

    private bool HasTranslation(string languageCode)
    {
        return _poi?.Translations.Any(translation =>
            string.Equals(translation.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase)) ?? false;
    }
}
