using NarrationApp.Shared.Visitor;

namespace NarrationApp.Mobile.Features.Home;

public sealed partial class VisitorShellState
{
    public void ToggleNotifications()
    {
        ShowNotifications = !ShowNotifications;
    }

    public void ApplyLiveNotifications(IReadOnlyList<VisitorNotification> notifications)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        var liveNotifications = notifications
            .Where(item => !string.IsNullOrWhiteSpace(item.Title))
            .Select(item => item with { IsLive = true })
            .ToArray();

        if (liveNotifications.Length == 0)
        {
            return;
        }

        var localNotifications = _notifications
            .Where(item => !item.IsLive)
            .ToArray();

        _notifications.Clear();
        _notifications.AddRange(liveNotifications);

        foreach (var notification in localNotifications)
        {
            AddNotificationIfUnique(notification);
        }

        TrimNotifications();
    }

    public void AddQrLaunchNotification(VisitorQrDeepLinkRequest request, VisitorQrNavigationTarget target)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(target);

        if (!string.Equals(request.QrLaunchMode, "strong0", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var body = target.Kind switch
        {
            VisitorQrTargetKind.PoiList => "Thiết bị được nhận diện cấu hình mạnh, đã mở danh sách POI trong app.",
            VisitorQrTargetKind.Poi => "Thiết bị được nhận diện cấu hình mạnh, đã mở POI trong app.",
            VisitorQrTargetKind.Tour => "Thiết bị được nhận diện cấu hình mạnh, đã mở tour trong app.",
            _ => "Thiết bị được nhận diện cấu hình mạnh, đã mở ứng dụng từ QR."
        };

        AddNotificationToTopIfUnique(new VisitorNotification("QR cấu hình mạnh", body, "Vừa xong"));
    }

    private void AddProximityNotification(VisitorProximityMatch proximity)
    {
        var title = $"Đang ở gần {proximity.PoiName}";
        var body = $"Đã vào bán kính {proximity.TriggerRadiusMeters}m, còn khoảng {proximity.DistanceMeters}m. Audio sẽ tự phát nếu đã sẵn sàng.";

        if (_notifications.FirstOrDefault() is { } latest
            && string.Equals(latest.Title, title, StringComparison.OrdinalIgnoreCase)
            && string.Equals(latest.Body, body, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _notifications.Insert(0, new VisitorNotification(title, body, "Vừa xong"));
        TrimNotifications();
    }

    private void AddNotificationToTopIfUnique(VisitorNotification notification)
    {
        if (_notifications.Any(item =>
            string.Equals(item.Title, notification.Title, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.Body, notification.Body, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _notifications.Insert(0, notification);
        TrimNotifications();
    }

    private void AddNotificationIfUnique(VisitorNotification notification)
    {
        if (_notifications.Any(item =>
            string.Equals(item.Title, notification.Title, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.Body, notification.Body, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _notifications.Add(notification);
    }

    private void TrimNotifications()
    {
        const int maxNotifications = 12;
        if (_notifications.Count > maxNotifications)
        {
            _notifications.RemoveRange(maxNotifications, _notifications.Count - maxNotifications);
        }
    }
}
