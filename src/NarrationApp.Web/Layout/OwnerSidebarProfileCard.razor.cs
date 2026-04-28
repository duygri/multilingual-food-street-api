using Microsoft.AspNetCore.Components;

namespace NarrationApp.Web.Layout;

public partial class OwnerSidebarProfileCard
{
    [Parameter]
    public string Initials { get; set; } = "OW";

    [Parameter]
    public string DisplayName { get; set; } = string.Empty;

    [Parameter]
    public string TotalPoisText { get; set; } = "—";

    [Parameter]
    public string PublishedPoisText { get; set; } = "—";

    [Parameter]
    public string PendingModerationText { get; set; } = "—";
}
