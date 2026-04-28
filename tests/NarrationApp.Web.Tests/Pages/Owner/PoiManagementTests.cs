using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Web.Pages.Owner;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class PoiManagementTests : TestContext
{
    [Fact]
    public void Poi_management_redirect_is_split_out_of_inline_code()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pageRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Owner");
        var markupPath = Path.Combine(pageRoot, "PoiManagement.razor");
        var codeBehindPath = Path.Combine(pageRoot, "PoiManagement.razor.cs");

        var markup = File.ReadAllText(markupPath);
        Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);
        Assert.True(File.Exists(codeBehindPath));
        Assert.Contains("partial class PoiManagement", File.ReadAllText(codeBehindPath), StringComparison.Ordinal);
    }

    [Fact]
    public void Legacy_route_redirects_to_owner_pois_without_rendering_giant_editor()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();

        var cut = RenderComponent<PoiManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("http://localhost/owner/pois", navigation.Uri);
            Assert.Contains("Đang chuyển sang danh sách POI", cut.Markup);
            Assert.Contains("/owner/pois", cut.Markup);
            Assert.DoesNotContain("Trình biên tập POI", cut.Markup);
            Assert.DoesNotContain("Vùng kích hoạt mặc định", cut.Markup);
            Assert.DoesNotContain("data-field=\"poi-name\"", cut.Markup);
        });
    }
}
