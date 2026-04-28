using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class UserManagement
{
    private async Task RefreshAsync()
    {
        if (_refreshCts is not null)
        {
            await LoadVisitorsAsync(showLoading: false, _refreshCts.Token);
        }
    }

    private async Task LoadVisitorsAsync(bool showLoading, CancellationToken cancellationToken)
    {
        if (showLoading) _isLoading = true;
        else _isRefreshing = true;

        try
        {
            _errorMessage = null;
            _visitors = await AdminPortalService.GetVisitorDevicesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ApiException exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _isLoading = false;
            _isRefreshing = false;
            if (!cancellationToken.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task RunRefreshLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(RefreshIntervalSeconds));
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await LoadVisitorsAsync(showLoading: false, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_refreshCts is not null)
        {
            await _refreshCts.CancelAsync();
            _refreshCts.Dispose();
        }

        try
        {
            await _refreshLoop;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
