using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task RefreshNotificationsAsync(bool force = false)
    {
        if (_isNotificationFeedLoading)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (!force
            && _lastNotificationFeedRefreshUtc is not null
            && now - _lastNotificationFeedRefreshUtc.Value < TimeSpan.FromSeconds(45))
        {
            return;
        }

        _isNotificationFeedLoading = true;
        try
        {
            var notifications = await NotificationFeedService.LoadAsync(
                new VisitorNotificationFeedRequest(_state.CurrentLocation, _state.SelectedLanguageCode));

            _state.ApplyLiveNotifications(notifications);
            _lastNotificationFeedRefreshUtc = now;
        }
        catch (Exception ex)
        {
            VisitorMobileDiagnostics.Log("Home", $"RefreshNotificationsAsync failed: {ex.Message}");
        }
        finally
        {
            _isNotificationFeedLoading = false;
        }
    }
}
