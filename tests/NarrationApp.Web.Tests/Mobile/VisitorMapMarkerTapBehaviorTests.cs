using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorMapMarkerTapBehaviorTests
{
    [Fact]
    public void First_tap_on_marker_opens_preview_sheet()
    {
        var decision = VisitorMapMarkerTapBehavior.Decide(
            selectedPoiId: "poi-oc-oanh",
            tappedPoiId: "poi-oc-dao",
            isPoiSheetOpen: true);

        Assert.Equal(VisitorMapMarkerTapAction.PreviewSheet, decision);
    }

    [Fact]
    public void Tapping_same_selected_marker_again_opens_full_detail()
    {
        var decision = VisitorMapMarkerTapBehavior.Decide(
            selectedPoiId: "poi-oc-oanh",
            tappedPoiId: "poi-oc-oanh",
            isPoiSheetOpen: true);

        Assert.Equal(VisitorMapMarkerTapAction.OpenDetail, decision);
    }

    [Fact]
    public void Tapping_same_marker_when_sheet_is_closed_reopens_preview_instead_of_detail()
    {
        var decision = VisitorMapMarkerTapBehavior.Decide(
            selectedPoiId: "poi-oc-oanh",
            tappedPoiId: "poi-oc-oanh",
            isPoiSheetOpen: false);

        Assert.Equal(VisitorMapMarkerTapAction.PreviewSheet, decision);
    }
}
