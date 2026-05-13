using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private IReadOnlyList<string> GetAboutTechStack() =>
    [
        "ASP.NET Core API",
        "Blazor Hybrid",
        ".NET 9",
        "Mapbox",
        "Google Cloud TTS",
        "Google Cloud Translation",
        "Cloudflare R2",
        "SignalR",
        "SQLite cache"
    ];

    private IReadOnlyList<VisitorAboutLinkItem> GetAboutLinks() =>
        UiText.AboutLinks();

    private Task OpenAboutLinkAsync(string label)
    {
        ShowSettingsFeedback(UiText.FormatAboutFeedback(label));
        return Task.CompletedTask;
    }

    private string GetAboutVersionLabel()
    {
        var version = typeof(Home).Assembly.GetName().Version;
        return version is null ? "Version local" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    private static string GetAboutRuntimeLabel() => ".NET 9 Android Smoke-ready";
}
