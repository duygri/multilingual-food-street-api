using Microsoft.AspNetCore.Components;

namespace NarrationApp.Web.Pages.Public;

public partial class QrLaunch
{
    [Parameter]
    public string Code { get; set; } = string.Empty;

    private string NormalizedCode => Uri.UnescapeDataString(Code ?? string.Empty).Trim();
    private string AppDeepLink => $"foodstreet://qr/{Uri.EscapeDataString(NormalizedCode)}";
}
