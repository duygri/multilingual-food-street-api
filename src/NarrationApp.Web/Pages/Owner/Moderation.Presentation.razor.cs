namespace NarrationApp.Web.Pages.Owner;

public partial class Moderation
{
    private static string GetPoiHref(int poiId) => $"/owner/pois/{poiId}";

    private static string GetDateTimeLabel(DateTime value) => value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    private static string GetResultClass(string result)
    {
        if (result.Contains("duyệt", StringComparison.OrdinalIgnoreCase))
        {
            return "owner-workspace-badge owner-workspace-badge--good";
        }

        if (result.Contains("từ chối", StringComparison.OrdinalIgnoreCase))
        {
            return "owner-workspace-badge owner-workspace-badge--danger";
        }

        return "owner-workspace-badge";
    }
}
