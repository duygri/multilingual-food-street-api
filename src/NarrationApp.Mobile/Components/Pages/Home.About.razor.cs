using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NarrationApp.Mobile.Components.Pages.Sections;
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
    [
        new VisitorAboutLinkItem("Điều khoản sử dụng", "Mở bản web hoặc help center để xem chi tiết."),
        new VisitorAboutLinkItem("Chính sách quyền riêng tư", "Giải thích cách app dùng vị trí và audio history."),
        new VisitorAboutLinkItem("Giấy phép mã nguồn mở", "Danh sách package và giấy phép đang dùng."),
        new VisitorAboutLinkItem("Gửi phản hồi", "Dùng khi cần báo bug hoặc góp ý bản visitor.")
    ];

    private Task OpenAboutLinkAsync(string label)
    {
        _profileStatusMessage = $"{label} sẽ nối sang web/support ở lượt hoàn thiện tiếp theo.";
        return Task.CompletedTask;
    }

    private string GetAboutVersionLabel()
    {
        var version = typeof(Home).Assembly.GetName().Version;
        return version is null ? "Version local" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    private static string GetAboutRuntimeLabel() => ".NET 9 Android Smoke-ready";
}
