using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task ProcessPendingDeepLinkAsync()
    {
        if (_isHandlingPendingDeepLink)
        {
            VisitorMobileDiagnostics.Log("Home", "ProcessPendingDeepLinkAsync skipped because handler is already running");
            return;
        }

        VisitorMobileDiagnostics.Log("Home", $"ProcessPendingDeepLinkAsync start currentStep={_state.CurrentStep} currentTab={_state.CurrentTab}");
        _isHandlingPendingDeepLink = true;

        try
        {
            while (true)
            {
                var request = VisitorPendingDeepLinkStore.Consume();
                if (request is null)
                {
                    VisitorMobileDiagnostics.Log("Home", "No more pending deep links");
                    break;
                }

                VisitorMobileDiagnostics.Log("Home", $"Processing deep link code={request.Code} source={request.SourceUri}");
                var resolution = await DeepLinkService.ResolveAsync(request);
                if (!resolution.Succeeded || resolution.NavigationTarget is null)
                {
                    VisitorMobileDiagnostics.Log("Home", resolution.ErrorMessage ?? $"Deep link failed for code={request.Code}");
                    continue;
                }

                await ApplyResolvedDeepLinkAsync(resolution);
            }
        }
        finally
        {
            _isHandlingPendingDeepLink = false;
            VisitorMobileDiagnostics.Log("Home", $"ProcessPendingDeepLinkAsync end currentStep={_state.CurrentStep} currentTab={_state.CurrentTab}");
        }
    }
}
