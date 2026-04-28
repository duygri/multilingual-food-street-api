using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void OpenQrModal()
    {
        _isQrModalOpen = true;
    }

    private void CloseQrModal()
    {
        _isQrModalOpen = false;
    }

    private VisitorPoi? GetQrPreviewPoi() =>
        VisitorQrPreviewSelector.GetPreviewPoi(_state.SelectedPoi, _state.FeaturedPois, _state.Pois);

    private string GetQrPreviewCode()
        => VisitorQrPreviewSelector.GetPreviewCode(GetQrPreviewPoi());

    private async Task TriggerQrPreviewAsync(VisitorQrTargetKind kind)
    {
        _isQrModalOpen = false;
        _discoverPoiDetailId = null;
        _tourDetailId = null;

        switch (kind)
        {
            case VisitorQrTargetKind.Poi when GetQrPreviewPoi() is { } poi:
                _state.ApplyQrNavigationTarget(new VisitorQrNavigationTarget($"PREVIEW-{poi.Id}", VisitorQrTargetKind.Poi, poi.Id));
                OpenDiscoverPoiDetailForQr(poi.Id);
                await PrepareSelectedPoiAudioAsync(autoPlay: true, forceAutoPlay: true);
                break;

            default:
                _state.SwitchTab(VisitorTab.Map);
                _state.ClosePoiSheet();
                break;
        }
    }
}
