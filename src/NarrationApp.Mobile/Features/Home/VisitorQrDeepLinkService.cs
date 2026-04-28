namespace NarrationApp.Mobile.Features.Home;

public interface IVisitorQrDeepLinkService
{
    Task<VisitorQrDeepLinkResolutionResult> ResolveAsync(
        VisitorQrDeepLinkRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record VisitorQrDeepLinkResolutionResult(
    VisitorQrDeepLinkRequest Request,
    string? DeviceId,
    VisitorQrNavigationTarget? NavigationTarget,
    string? ErrorMessage)
{
    public bool Succeeded => NavigationTarget is not null;
}

public sealed class VisitorQrDeepLinkService(
    IVisitorDeviceIdentityProvider deviceIdentityProvider,
    IVisitorQrApiService qrApiService) : IVisitorQrDeepLinkService
{
    public async Task<VisitorQrDeepLinkResolutionResult> ResolveAsync(
        VisitorQrDeepLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? deviceId = null;

        try
        {
            deviceId = await deviceIdentityProvider.GetDeviceIdAsync(cancellationToken);
            var qrCode = await qrApiService.OpenAsync(request.Code, deviceId, cancellationToken);
            var navigationTarget = VisitorQrNavigationTarget.FromQrCode(qrCode);
            return new VisitorQrDeepLinkResolutionResult(request, deviceId, navigationTarget, null);
        }
        catch (VisitorApiException ex)
        {
            return new VisitorQrDeepLinkResolutionResult(
                request,
                deviceId,
                null,
                $"Không mở được QR {request.Code}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new VisitorQrDeepLinkResolutionResult(
                request,
                deviceId,
                null,
                $"Không xử lý được deep link: {ex.Message}");
        }
    }
}
