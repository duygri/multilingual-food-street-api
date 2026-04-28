using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class ModerationQueue
{
    private async Task ReloadAsync()
    {
        try
        {
            _items = (await AdminPortalService.GetPendingModerationAsync()).ToList();
        }
        catch (ApiException exception)
        {
            _errorMessage = exception.Message;
        }
    }

    private async Task ReviewAsync(int requestId, bool approved)
    {
        try
        {
            var request = new ReviewModerationRequest
            {
                ReviewNote = approved ? "Đã duyệt từ hàng chờ kiểm duyệt." : "Đã từ chối từ hàng chờ kiểm duyệt."
            };

            if (approved)
            {
                await AdminPortalService.ApproveModerationAsync(requestId, request);
                _statusMessage = $"Yêu cầu kiểm duyệt #{requestId} đã được duyệt.";
            }
            else
            {
                await AdminPortalService.RejectModerationAsync(requestId, request);
                _statusMessage = $"Yêu cầu kiểm duyệt #{requestId} đã bị từ chối.";
            }

            _items = _items.Where(item => item.Id != requestId).ToList();
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }
}
