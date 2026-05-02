using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task ApplyResolvedDeepLinkAsync(VisitorQrDeepLinkResolutionResult resolution)
    {
        VisitorMobileDiagnostics.Log("Home", $"Resolved device id={resolution.DeviceId ?? "<null>"}");
        VisitorMobileDiagnostics.Log(
            "Home",
            $"Navigation target kind={resolution.NavigationTarget!.Kind} targetId={resolution.NavigationTarget.TargetId ?? "<null>"}");

        _lastAutoPlayedPoiId = null;
        _isAutoPlayingFromProximity = false;
        _state.ApplyQrNavigationTarget(resolution.NavigationTarget);
        VisitorMobileDiagnostics.Log(
            "Home",
            $"State after apply currentStep={_state.CurrentStep} currentTab={_state.CurrentTab} selectedPoi={_state.SelectedPoi?.Id ?? "<null>"} selectedTour={_state.SelectedTour?.Id ?? "<null>"}");

        if (resolution.NavigationTarget.Kind == VisitorQrTargetKind.Poi && _state.SelectedPoi is not null)
        {
            OpenDiscoverPoiDetailForQr(_state.SelectedPoi.Id);
            await HandlePoiDeepLinkAudioAsync();
        }
    }

    private void OpenDiscoverPoiDetailForQr(string poiId)
    {
        ShowDiscoverPoiDetail(poiId);
    }

    private async Task HandlePoiDeepLinkAudioAsync()
    {
        if (_audioBridge is null)
        {
            _pendingSelectedPoiAudioPreparationRequested = true;
            _pendingSelectedPoiAutoPlay = true;
            VisitorMobileDiagnostics.Log("Home", $"Queued audio preparation for poi={_state.SelectedPoi!.Id} autoPlay=true");
            return;
        }

        VisitorMobileDiagnostics.Log("Home", $"Preparing audio immediately for poi={_state.SelectedPoi!.Id} autoPlay=true");
        await PrepareSelectedPoiAudioAsync(autoPlay: true, forceAutoPlay: true);
    }
}
