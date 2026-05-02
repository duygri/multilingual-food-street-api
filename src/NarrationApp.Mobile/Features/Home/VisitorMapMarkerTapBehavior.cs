namespace NarrationApp.Mobile.Features.Home;

public enum VisitorMapMarkerTapAction
{
    PreviewSheet,
    OpenDetail
}

public static class VisitorMapMarkerTapBehavior
{
    public static VisitorMapMarkerTapAction Decide(
        string? selectedPoiId,
        string tappedPoiId,
        bool isPoiSheetOpen)
    {
        if (isPoiSheetOpen
            && !string.IsNullOrWhiteSpace(selectedPoiId)
            && string.Equals(selectedPoiId, tappedPoiId, StringComparison.OrdinalIgnoreCase))
        {
            return VisitorMapMarkerTapAction.OpenDetail;
        }

        return VisitorMapMarkerTapAction.PreviewSheet;
    }
}
