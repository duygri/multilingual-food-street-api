using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void QueueStartupWork()
    {
        if (_startupWorkQueued)
        {
            return;
        }

        _startupWorkQueued = true;
        _ = InvokeAsync(RunStartupWorkAsync);
    }

    private async Task RunStartupWorkAsync()
    {
        // Yield once so the first frame can paint before network/session sync starts.
        await Task.Yield();
        VisitorMobileDiagnostics.Log("Home", "RunStartupWorkAsync start");

        try
        {
            await LoadContentAsync();
            VisitorMobileDiagnostics.Log("Home", $"Content loaded; currentStep={_state.CurrentStep} pois={_state.Pois.Count} tours={_state.Tours.Count}");
            SyncProfileDraftFromSession();

            await ProcessPendingDeepLinkAsync();
            VisitorMobileDiagnostics.Log("Home", $"RunStartupWorkAsync end currentStep={_state.CurrentStep} currentTab={_state.CurrentTab}");
        }
        catch (Exception ex)
        {
            VisitorMobileDiagnostics.Log("Home", $"RunStartupWorkAsync failed: {ex}");
            _profileErrorMessage ??= ex.Message;
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private void HandlePendingDeepLinkChanged()
    {
        VisitorMobileDiagnostics.Log("Home", "PendingChanged event received");
        _ = InvokeAsync(async () =>
        {
            await ProcessPendingDeepLinkAsync();
            StateHasChanged();
        });
    }
}
