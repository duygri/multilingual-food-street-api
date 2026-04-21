using System.Diagnostics;

namespace NarrationApp.Mobile.Features.Home;

internal static class TouristMobileDiagnostics
{
    public static void Log(string scope, string message)
    {
        var line = $"[TouristMobile][{DateTimeOffset.UtcNow:O}][{scope}] {message}";
        Debug.WriteLine(line);
        Console.WriteLine(line);
    }
}
