using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Owner;

public partial class Notifications : IDisposable
{
    private readonly NotificationType[] _notificationTypes = Enum.GetValues<NotificationType>();
    private bool _isLoading = true;
    private bool _isUpdating;
    private string? _errorMessage;
    private string? _statusMessage;
    private string _readFilter = "all";
    private string _typeFilter = "all";
    private int _unreadCount;
    private IReadOnlyList<NotificationDto> _items = Array.Empty<NotificationDto>();
    private IReadOnlyList<NotificationDto> FilteredItems => _items.Where(MatchesReadFilter).Where(MatchesTypeFilter).OrderByDescending(item => item.CreatedAtUtc).ToArray();

    protected override async Task OnInitializedAsync()
    {
        NotificationCenterService.Changed += HandleChanged;
        await ReloadAsync();
        _isLoading = false;
    }

    public void Dispose()
    {
        NotificationCenterService.Changed -= HandleChanged;
    }
}
