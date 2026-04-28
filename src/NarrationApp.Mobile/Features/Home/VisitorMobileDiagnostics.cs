using System.Diagnostics;

namespace NarrationApp.Mobile.Features.Home;

internal static class VisitorMobileDiagnostics
{
    [Conditional("DEBUG")]
    [Conditional("SMOKE")]
    public static void Log(string scope, string message)
    {
        var line = $"[VisitorMobile][{DateTimeOffset.UtcNow:O}][{scope}] {message}";
        Debug.WriteLine(line);
    }
}
