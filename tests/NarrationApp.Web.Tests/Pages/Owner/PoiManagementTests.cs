using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Web.Pages.Owner;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class PoiManagementTests : TestContext
{
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
