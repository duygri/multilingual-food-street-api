using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    protected override void OnInitialized()
    {
        VisitorMobileDiagnostics.Log("Home", "OnInitialized start");
        VisitorPendingDeepLinkStore.PendingChanged += HandlePendingDeepLinkChanged;
        VisitorMobileDiagnostics.Log("Home", $"OnInitialized end currentStep={_state.CurrentStep} currentTab={_state.CurrentTab}");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _mapBridge = DotNetObjectReference.Create(this);
            _audioBridge = DotNetObjectReference.Create(this);
            StartPresenceHeartbeatLoopIfNeeded();
            StartForegroundLocationLoopIfNeeded();
            QueueStartupWork();
        }

        if (_pendingSelectedPoiAudioPreparationRequested && _audioBridge is not null)
        {
            var autoPlay = _pendingSelectedPoiAutoPlay;
            _pendingSelectedPoiAudioPreparationRequested = false;
            _pendingSelectedPoiAutoPlay = false;
            await PrepareSelectedPoiAudioAsync(autoPlay, forceAutoPlay: autoPlay);
            StateHasChanged();
        }

        await RenderMapIfNeededAsync();
    }
}
