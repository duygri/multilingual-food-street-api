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

    [Fact]
    public void Renders_generic_sidebar_profile_region_between_brand_and_navigation()
    {
        var cut = RenderComponent<PortalShell>(parameters => parameters
            .Add(parameter => parameter.Brand, "Vĩnh Khánh Owner")
            .Add(parameter => parameter.SidebarProfileContent, (RenderFragment)(builder => builder.AddMarkupContent(0, """
                <section class="owner-profile-card" data-testid="owner-profile-card">
                    <strong>Demo Owner</strong>
                    <span>POI Owner</span>
                </section>
                """)))
            .Add(parameter => parameter.NavigationItems,
            [
                new ShellNavItem
                {
                    Group = "Tổng quan",
                    Label = "Dashboard",
                    Href = "/owner/dashboard"
                }
            ]));

        var profileCard = cut.Find("[data-testid='owner-profile-card']");
        var brandIndex = cut.Markup.IndexOf("portal-shell__brand", StringComparison.Ordinal);
        var profileIndex = cut.Markup.IndexOf("data-testid=\"owner-profile-card\"", StringComparison.Ordinal);
        var navIndex = cut.Markup.IndexOf("portal-shell__nav", StringComparison.Ordinal);

        Assert.Contains("Demo Owner", profileCard.TextContent);
        Assert.True(brandIndex < profileIndex);
        Assert.True(profileIndex < navIndex);
    }

    [Theory]
    [InlineData("http://localhost/owner/pois", "POI")]
    [InlineData("http://localhost/owner/pois/new", "Tạo POI mới")]
    [InlineData("http://localhost/owner/pois/42", "POI")]
    public void Owner_poi_navigation_uses_exact_item_before_prefix_item(string uri, string expectedActiveLabel)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(uri);

        var cut = RenderComponent<PortalShell>(parameters => parameters
            .Add(parameter => parameter.Brand, "Vĩnh Khánh Owner")
            .Add(parameter => parameter.NavigationItems,
            [
                new ShellNavItem
                {
                    Group = "Nội dung",
                    Label = "POI",
                    Href = "/owner/pois",
                    Match = ShellNavItemMatch.Prefix
                },
                new ShellNavItem
                {
                    Group = "Nội dung",
                    Label = "Tạo POI mới",
                    Href = "/owner/pois/new",
                    Match = ShellNavItemMatch.Exact
                }
            ]));

        var activeLabels = cut.FindAll(".portal-shell__nav-link--active .portal-shell__nav-label")
            .Select(element => element.TextContent.Trim())
            .ToArray();

        Assert.Equal([expectedActiveLabel], activeLabels);
    }

    [Fact]
    public void Renders_owner_navigation_groups_and_badges()
    {
        var cut = RenderComponent<PortalShell>(parameters => parameters
            .Add(parameter => parameter.Brand, "Vĩnh Khánh Owner")
            .Add(parameter => parameter.NavigationItems,
            [
                new ShellNavItem { Group = "Tổng quan", Label = "Dashboard", Href = "/owner/dashboard" },
                new ShellNavItem { Group = "Nội dung", Label = "POI", Href = "/owner/pois" },
                new ShellNavItem { Group = "Vận hành", Label = "Moderation", Href = "/owner/moderation", BadgeText = "3" },
                new ShellNavItem { Group = "Vận hành", Label = "Notifications", Href = "/owner/notifications", BadgeText = "7" },
                new ShellNavItem { Group = "Tài khoản", Label = "Profile", Href = "/owner/profile" }
            ]));

        Assert.Contains("Tổng quan", cut.Markup);
        Assert.Contains("Nội dung", cut.Markup);
        Assert.Contains("Vận hành", cut.Markup);
        Assert.Contains("Tài khoản", cut.Markup);
        Assert.Equal(["3", "7"], cut.FindAll(".portal-shell__nav-badge").Select(badge => badge.TextContent.Trim()));
    }
}
