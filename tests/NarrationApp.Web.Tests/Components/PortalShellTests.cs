using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.SharedUI.Components;
using NarrationApp.SharedUI.Models;

namespace NarrationApp.Web.Tests.Components;

public sealed class PortalShellTests : TestContext
{
    [Fact]
    public void Renders_grouped_sidebar_with_explicit_active_navigation_item_style()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/admin/moderation-queue");

        var cut = RenderComponent<PortalShell>(parameters => parameters
            .Add(parameter => parameter.Brand, "Vĩnh Khánh Admin")
            .Add(parameter => parameter.Eyebrow, "v2.1 — Quản trị hệ thống")
            .Add(parameter => parameter.NavigationItems,
            [
                new ShellNavItem
                {
                    Group = "Tổng quan",
                    Label = "Dashboard",
                    Href = "/admin/dashboard",
                    Description = "Moderation & users",
                    IconGlyph = "◈"
                },
                new ShellNavItem
                {
                    Group = "Điều hành",
                    Label = "Moderation",
                    Href = "/admin/moderation-queue",
                    Description = "Approve & reject",
                    IconGlyph = "▣",
                    BadgeText = "5"
                }
            ])
            .Add(parameter => parameter.HeaderContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Header</div>")))
            .Add(parameter => parameter.ChildContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        Assert.Contains("Vĩnh Khánh Admin", cut.Markup);
        Assert.Contains("v2.1 — Quản trị hệ thống", cut.Markup);
        Assert.Contains("Tổng quan", cut.Markup);
        Assert.Contains("Điều hành", cut.Markup);
        Assert.Contains("5", cut.Markup);
        Assert.Contains("portal-shell__nav-link--plain", cut.Markup);
        Assert.Contains("portal-shell__nav-link active", cut.Markup);
        Assert.Contains("portal-shell__nav-link--active", cut.Markup);
        Assert.DoesNotContain("portal-shell__nav-icon", cut.Markup);
        Assert.DoesNotContain("◈", cut.Markup);
        Assert.DoesNotContain("▣", cut.Markup);
        Assert.DoesNotContain("Moderation & users", cut.Markup);
        Assert.DoesNotContain("Approve & reject", cut.Markup);
    }
}
