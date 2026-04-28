using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.DTOs.QR;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class Dashboard
{
    private bool _isLoading = true;
    private string? _errorMessage;
    private DashboardDto _overview = new();
    private AudioPlayAnalyticsDto _audioAnalytics = new();
    private IReadOnlyList<UserSummaryDto> _users = Array.Empty<UserSummaryDto>();
    private IReadOnlyList<VisitorDeviceSummaryDto> _visitorDevices = Array.Empty<VisitorDeviceSummaryDto>();
    private IReadOnlyList<ModerationRequestDto> _pendingModeration = Array.Empty<ModerationRequestDto>();
    private IReadOnlyList<QrCodeDto> _qrItems = Array.Empty<QrCodeDto>();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var overviewTask = AdminPortalService.GetOverviewAsync();
            var usersTask = AdminPortalService.GetUsersAsync();
            var visitorDevicesTask = AdminPortalService.GetVisitorDevicesAsync();
            var moderationTask = AdminPortalService.GetPendingModerationAsync();
            var audioTask = AdminPortalService.GetAudioPlayAnalyticsAsync();
            var qrTask = QrPortalService.GetAsync();
            await Task.WhenAll(overviewTask, usersTask, visitorDevicesTask, moderationTask, audioTask, qrTask);
            _overview = overviewTask.Result;
            _users = usersTask.Result;
            _visitorDevices = visitorDevicesTask.Result;
            _pendingModeration = moderationTask.Result;
            _audioAnalytics = audioTask.Result;
            _qrItems = qrTask.Result;
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
}
