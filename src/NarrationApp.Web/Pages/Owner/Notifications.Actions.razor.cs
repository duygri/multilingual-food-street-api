using NarrationApp.Shared.DTOs.Notification;

namespace NarrationApp.Web.Pages.Owner;

public partial class Notifications
{
    private async Task ReloadAsync()
    {
        try
        {
            _items = await NotificationCenterService.GetAsync();
            _unreadCount = await NotificationCenterService.GetUnreadCountAsync();
            _errorMessage = null;
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
        }
    }

    private async Task MarkReadAsync(NotificationDto item)
    {
        _isUpdating = true;
        try
        {
            await NotificationCenterService.MarkReadAsync(item.Id);
            await ReloadAsync();
            _statusMessage = $"Đã đánh dấu {item.Title} là đã đọc.";
        }
        catch (Exception exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private async Task MarkAllReadAsync()
    {
        _isUpdating = true;
        try
        {
            await NotificationCenterService.MarkAllReadAsync();
            await ReloadAsync();
            _statusMessage = "Đã đánh dấu tất cả thông báo là đã đọc.";
        }
        catch (Exception exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private async void HandleChanged()
    {
        await InvokeAsync(async () =>
        {
            await ReloadAsync();
            StateHasChanged();
        });
    }
}
