using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Enums;
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
        await AddQrPreviewAudioAsync(factory, qr.TargetId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/qr/{qr.Code}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Nghe thuyết minh", html, StringComparison.Ordinal);
        Assert.Contains("Audio đa ngôn ngữ", html, StringComparison.Ordinal);
        Assert.Contains("Ốc Oanh", html, StringComparison.Ordinal);
        Assert.Contains("/api/audio/501/stream", html, StringComparison.Ordinal);
        Assert.Contains("/api/audio/502/stream", html, StringComparison.Ordinal);
        Assert.Contains(">vi<", html, StringComparison.Ordinal);
        Assert.Contains(">en<", html, StringComparison.Ordinal);
        Assert.Contains($"foodstreet://qr/{qr.Code}", html, StringComparison.Ordinal);
        Assert.Contains(qr.Code, html, StringComparison.Ordinal);
        Assert.Contains("Mở ứng dụng", html, StringComparison.Ordinal);
        Assert.Contains($"window.location.href = \"foodstreet://qr/{qr.Code}\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_qr_route_returns_open_app_launcher_for_open_app_target()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.SeedAsync();

        var qr = await CreateOpenAppQrAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/qr/{qr.Code}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("QR mở app", html, StringComparison.Ordinal);
        Assert.Contains("Launcher", html, StringComparison.Ordinal);
        Assert.Contains($"foodstreet://qr/{qr.Code}", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Audio đa ngôn ngữ", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Nghe thuyết minh", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_qr_route_returns_tour_launcher_with_appless_web_fallback()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.SeedAsync();

        var qr = await CreateTourQrAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/qr/{qr.Code}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("QR mở tour", html, StringComparison.Ordinal);
        Assert.Contains("Nếu chưa cài app", html, StringComparison.Ordinal);
        Assert.Contains($"foodstreet://qr/{qr.Code}", html, StringComparison.Ordinal);
        Assert.Contains($"window.location.href = \"foodstreet://qr/{qr.Code}\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_qr_route_returns_poi_list_with_appless_audio_language_fallback()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.SeedAsync();

        var qr = await CreatePoiListQrAsync(factory);
        var poiId = await GetFirstPublishedPoiIdAsync(factory);
        await AddQrPreviewAudioAsync(factory, poiId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/qr/{qr.Code}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("QR danh sách POI", html, StringComparison.Ordinal);
        Assert.Contains("Danh sách điểm tham quan", html, StringComparison.Ordinal);
        Assert.Contains("Ốc Oanh", html, StringComparison.Ordinal);
        Assert.Contains("/api/audio/501/stream", html, StringComparison.Ordinal);
        Assert.Contains("/api/audio/502/stream", html, StringComparison.Ordinal);
        Assert.Contains(">vi<", html, StringComparison.Ordinal);
        Assert.Contains(">en<", html, StringComparison.Ordinal);
        Assert.Contains($"foodstreet://qr/{qr.Code}", html, StringComparison.Ordinal);
        Assert.Contains("data-poi-card", html, StringComparison.Ordinal);
        Assert.Contains("data-audio-src", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_qr_route_embeds_scan_tracking_for_visitor_devices()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.SeedAsync();

        var qr = await CreatePoiQrAsync(factory);
        await AddQrPreviewAudioAsync(factory, qr.TargetId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/qr/{qr.Code}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains($"/api/qr/{qr.Code}/scan", html, StringComparison.Ordinal);
        Assert.Contains($"/api/qr/{qr.Code}/presence", html, StringComparison.Ordinal);
        Assert.Contains("X-Device-Id", html, StringComparison.Ordinal);
        Assert.Contains("localStorage", html, StringComparison.Ordinal);
        Assert.Contains("qr-web-", html, StringComparison.Ordinal);
        Assert.Contains("trackPublicQrPresence", html, StringComparison.Ordinal);
        Assert.Contains("4000", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Poi_list_qr_scan_registers_qr_web_device_as_online_for_admin_dashboard()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.SeedAsync();

        var qr = await CreatePoiListQrAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/qr/{qr.Code}/scan");
        request.Headers.Add("X-Device-Id", "qr-web-poi-list-dashboard-001");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dashboardService = scope.ServiceProvider.GetRequiredService<IVisitorDeviceDashboardService>();
        var devices = await dashboardService.GetAsync();
        var device = Assert.Single(devices.Where(item => item.DeviceId == "qr-web-poi-list-dashboard-001"));

        Assert.True(device.IsOnline);
        Assert.Equal("guest", device.RoleName);
        Assert.False(device.AutoPlayEnabled);
        Assert.False(device.BackgroundTrackingEnabled);
    }

    [Fact]
    public async Task Public_poi_list_qr_route_uses_server_device_config_zero_to_open_app()
    {
        await using var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["QrDeviceConfig:ForcedValue"] = "0"
        });
        await factory.SeedAsync();

        var qr = await CreatePoiListQrAsync(factory);
        var poiId = await GetFirstPublishedPoiIdAsync(factory);
        await AddQrPreviewAudioAsync(factory, poiId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/qr/{qr.Code}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("QR danh sách POI", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Math.floor(Math.random() * 2)", html, StringComparison.Ordinal);
        Assert.Contains("0 = cấu hình mạnh", html, StringComparison.Ordinal);
        Assert.Contains("1 = cấu hình yếu", html, StringComparison.Ordinal);
        Assert.Contains("data-qr-device-config=\"0\"", html, StringComparison.Ordinal);
        Assert.Contains("Cấu hình mạnh (0)", html, StringComparison.Ordinal);
        Assert.Contains("App sẽ hiển thị thông báo QR cấu hình mạnh", html, StringComparison.Ordinal);
        Assert.Contains($"window.location.href = \"foodstreet://qr/{qr.Code}?qrLaunch=strong0\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_poi_list_qr_route_uses_server_device_config_one_for_web_fallback()
    {
        await using var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["QrDeviceConfig:ForcedValue"] = "1"
        });
        await factory.SeedAsync();

        var qr = await CreatePoiListQrAsync(factory);
        var poiId = await GetFirstPublishedPoiIdAsync(factory);
        await AddQrPreviewAudioAsync(factory, poiId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/qr/{qr.Code}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Math.floor(Math.random() * 2)", html, StringComparison.Ordinal);
        Assert.Contains("data-qr-device-config=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("Cấu hình yếu (1)", html, StringComparison.Ordinal);
        Assert.Contains("Máy yếu nên giữ bản web fallback", html, StringComparison.Ordinal);
        Assert.DoesNotContain("window.location.href = \"foodstreet://qr/", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_share_poi_route_returns_social_preview_without_qr_code()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.SeedAsync();

        var poiId = await GetFirstPublishedPoiIdAsync(factory);
        await AddQrPreviewAudioAsync(factory, poiId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://public.foodstreet.test"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/share/poi/{poiId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("og:title", html, StringComparison.Ordinal);
        Assert.Contains("Public share", html, StringComparison.Ordinal);
        Assert.Contains("Ốc Oanh", html, StringComparison.Ordinal);
        Assert.Contains("/api/audio/501/stream", html, StringComparison.Ordinal);
        Assert.Contains("https://public.foodstreet.test/share/poi/", html, StringComparison.Ordinal);
        Assert.DoesNotContain("foodstreet://qr/", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Assetlinks_endpoint_returns_android_associations_for_verified_qr_domain()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.SeedAsync();

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://public.foodstreet.test")
        });

        var response = await client.GetAsync("/.well-known/assetlinks.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("delegate_permission/common.handle_all_urls", json, StringComparison.Ordinal);
        Assert.Contains("com.foodstreet.tourist.dev", json, StringComparison.Ordinal);
        Assert.Contains("11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Assetlinks_endpoint_omits_android_targets_with_placeholder_fingerprints()
    {
        await using var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["MobileAppLinks:Android:1:PackageName"] = "com.foodstreet.tourist",
            ["MobileAppLinks:Android:1:Sha256CertFingerprints:0"] = "REPLACE_WITH_RELEASE_SHA256_CERT_FINGERPRINT"
        });
        await factory.SeedAsync();

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://public.foodstreet.test")
        });

        var response = await client.GetAsync("/.well-known/assetlinks.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"package_name\":\"com.foodstreet.tourist.dev\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"package_name\":\"com.foodstreet.tourist\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("REPLACE_WITH_", json, StringComparison.Ordinal);
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

    private static async Task<QrCodeDto> CreateOpenAppQrAsync(TestWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var qrService = scope.ServiceProvider.GetRequiredService<IQrService>();

        return await qrService.CreateAsync(new CreateQrRequest
        {
            TargetType = "open_app",
            TargetId = 0,
            LocationHint = "Integration launcher"
        });
    }

    private static async Task<QrCodeDto> CreateTourQrAsync(TestWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var qrService = scope.ServiceProvider.GetRequiredService<IQrService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tour = dbContext.Tours.OrderBy(item => item.Id).First();

        return await qrService.CreateAsync(new CreateQrRequest
        {
            TargetType = "tour",
            TargetId = tour.Id,
            LocationHint = "Integration tour gate"
        });
    }

    private static async Task<QrCodeDto> CreatePoiListQrAsync(TestWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var qrService = scope.ServiceProvider.GetRequiredService<IQrService>();

        return await qrService.CreateAsync(new CreateQrRequest
        {
            TargetType = "poi_list",
            TargetId = 0,
            LocationHint = "Danh sách POI Vĩnh Khánh"
        });
    }

    private static async Task<int> GetFirstPublishedPoiIdAsync(TestWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Pois
            .Where(item => item.Status == PoiStatus.Published)
            .OrderBy(item => item.Id)
            .Select(item => item.Id)
            .FirstAsync();
    }

    private static async Task AddQrPreviewAudioAsync(TestWebApplicationFactory factory, int poiId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.AudioAssets.AddRange(
        [
            new AudioAsset
            {
                Id = 501,
                PoiId = poiId,
                LanguageCode = "vi",
                SourceType = AudioSourceType.Tts,
                Provider = "integration-google-tts",
                StoragePath = "audio/vi.mp3",
                Url = "/api/audio/501/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 5,
                GeneratedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new AudioAsset
            {
                Id = 502,
                PoiId = poiId,
                LanguageCode = "en",
                SourceType = AudioSourceType.Tts,
                Provider = "integration-google-tts",
                StoragePath = "audio/en.mp3",
                Url = "/api/audio/502/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 5,
                GeneratedAt = DateTime.UtcNow.AddMinutes(-4)
            }
        ]);

        await dbContext.SaveChangesAsync();
    }
}
