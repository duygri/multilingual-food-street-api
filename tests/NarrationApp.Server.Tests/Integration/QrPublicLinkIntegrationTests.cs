using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Server.Tests.Integration;

public sealed class QrPublicLinkIntegrationTests
{
    [Fact]
    public async Task Resolve_endpoint_returns_public_url_and_app_deep_link()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.SeedAsync();

        var qr = await CreatePoiQrAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync($"/api/qr/{qr.Code}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<QrCodeDto>>();
        var resolved = Assert.IsType<QrCodeDto>(envelope?.Data);
        Assert.Equal($"https://public.foodstreet.test/qr/{qr.Code}", resolved.PublicUrl);
        Assert.Equal($"foodstreet://qr/{qr.Code}", resolved.AppDeepLink);
    }

    [Fact]
    public async Task Public_qr_route_returns_launch_page_for_existing_code()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.SeedAsync();

        var qr = await CreatePoiQrAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/qr/{qr.Code}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains($"foodstreet://qr/{qr.Code}", html, StringComparison.Ordinal);
        Assert.Contains(qr.Code, html, StringComparison.Ordinal);
        Assert.Contains("Mở Food Street", html, StringComparison.Ordinal);
    }

    private static async Task<QrCodeDto> CreatePoiQrAsync(TestWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var qrService = scope.ServiceProvider.GetRequiredService<IQrService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<NarrationApp.Server.Data.AppDbContext>();
        var poi = dbContext.Pois.OrderBy(item => item.Id).First();

        return await qrService.CreateAsync(new CreateQrRequest
        {
            TargetType = "poi",
            TargetId = poi.Id,
            LocationHint = "Integration gate"
        });
    }
}
