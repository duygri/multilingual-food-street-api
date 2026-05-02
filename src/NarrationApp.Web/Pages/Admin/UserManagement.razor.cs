using NarrationApp.Shared.DTOs.Admin;

namespace NarrationApp.Web.Pages.Admin;

public partial class UserManagement : IAsyncDisposable
{
    private const int RefreshIntervalSeconds = 5;
    private bool _isLoading = true;
    private bool _isRefreshing;
    private string? _errorMessage;
    private IReadOnlyList<VisitorDeviceSummaryDto> _visitors = Array.Empty<VisitorDeviceSummaryDto>();
    private CancellationTokenSource? _refreshCts;
    private Task _refreshLoop = Task.CompletedTask;
    private IReadOnlyList<VisitorDeviceSummaryDto> Visitors => _visitors;
    private IReadOnlyList<VisitorDeviceSummaryDto> OnlineVisitors => Visitors.Where(item => item.IsOnline).ToArray();
    private int ActiveVisitorCount => Visitors.Count(item => item.IsOnline);

    protected override async Task OnInitializedAsync()
    {
        _refreshCts = new CancellationTokenSource();
        await LoadVisitorsAsync(showLoading: true, _refreshCts.Token);
        _refreshLoop = RunRefreshLoopAsync(_refreshCts.Token);
    }
}
