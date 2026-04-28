namespace NarrationApp.Mobile.Features.Home;

public static class VisitorQrPreviewSelector
{
    public static VisitorPoi? GetPreviewPoi(
        VisitorPoi? selectedPoi,
        IReadOnlyList<VisitorPoi> featuredPois,
        IReadOnlyList<VisitorPoi> pois) =>
        selectedPoi ?? featuredPois.FirstOrDefault() ?? pois.FirstOrDefault();

    public static string GetPreviewCode(VisitorPoi? poi)
    {
        if (poi is not null)
        {
            return poi.Id.Replace("poi-", string.Empty, StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
        }

        return "OPEN-APP";
    }
}
