using System.Net;
using NarrationApp.Mobile.Features.Home;
using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorQrDeepLinkServiceTests
{
    [Fact]
    public async Task ResolveAsync_ReturnsNavigationTargetAndDeviceIdWhenQrOpenSucceeds()
    {
        var service = new VisitorQrDeepLinkService(
            new FakeDeviceIdentityProvider("device-123"),
            new FakeQrApiService(
                new QrCodeDto
                {
                    Code = "QR-001",
                    TargetType = "poi",
                    TargetId = 7
                }));

        var result = await service.ResolveAsync(new VisitorQrDeepLinkRequest("QR-001", "https://narration.app/qr/QR-001"));

        Assert.True(result.Succeeded);
        Assert.Equal("device-123", result.DeviceId);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.NavigationTarget);
        Assert.Equal(VisitorQrTargetKind.Poi, result.NavigationTarget!.Kind);
        Assert.Equal("poi-7", result.NavigationTarget.TargetId);
    }

    [Fact]
    public async Task ResolveAsync_FormatsQrApiErrorsForUi()
    {
        var service = new VisitorQrDeepLinkService(
            new FakeDeviceIdentityProvider("device-123"),
            new ThrowingQrApiService(new VisitorApiException("QR đã hết hạn.", HttpStatusCode.Gone)));

        var result = await service.ResolveAsync(new VisitorQrDeepLinkRequest("QR-OLD", "https://narration.app/qr/QR-OLD"));

        Assert.False(result.Succeeded);
        Assert.Equal("device-123", result.DeviceId);
        Assert.Null(result.NavigationTarget);
        Assert.Equal("Không mở được QR QR-OLD: QR đã hết hạn.", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveAsync_FormatsUnexpectedErrorsForUi()
    {
        var service = new VisitorQrDeepLinkService(
            new ThrowingDeviceIdentityProvider(new InvalidOperationException("identity unavailable")),
            new FakeQrApiService(new QrCodeDto()));

        var result = await service.ResolveAsync(new VisitorQrDeepLinkRequest("QR-ERR", "https://narration.app/qr/QR-ERR"));

        Assert.False(result.Succeeded);
        Assert.Null(result.NavigationTarget);
        Assert.Equal("Không xử lý được deep link: identity unavailable", result.ErrorMessage);
    }

    private sealed class FakeDeviceIdentityProvider(string deviceId) : IVisitorDeviceIdentityProvider
    {
        public ValueTask<string> GetDeviceIdAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(deviceId);
    }

    private sealed class ThrowingDeviceIdentityProvider(Exception exception) : IVisitorDeviceIdentityProvider
    {
        public ValueTask<string> GetDeviceIdAsync(CancellationToken cancellationToken = default) => ValueTask.FromException<string>(exception);
    }

    private sealed class FakeQrApiService(QrCodeDto qrCode) : IVisitorQrApiService
    {
        public Task<QrCodeDto> OpenAsync(string code, string deviceId, CancellationToken cancellationToken = default) => Task.FromResult(qrCode);
    }

    private sealed class ThrowingQrApiService(Exception exception) : IVisitorQrApiService
    {
        public Task<QrCodeDto> OpenAsync(string code, string deviceId, CancellationToken cancellationToken = default) => Task.FromException<QrCodeDto>(exception);
    }
}
