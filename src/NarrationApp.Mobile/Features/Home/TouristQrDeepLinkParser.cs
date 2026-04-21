using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Mobile.Features.Home;

public enum TouristQrTargetKind
{
    OpenApp,
    Poi,
    Tour
}

public sealed record TouristQrDeepLinkRequest(string Code, string SourceUri);

public sealed record TouristQrNavigationTarget(string Code, TouristQrTargetKind Kind, string? TargetId)
{
    public static TouristQrNavigationTarget FromQrCode(QrCodeDto qrCode)
    {
        var targetType = qrCode.TargetType.Trim().ToLowerInvariant();

        return targetType switch
        {
            "poi" when qrCode.TargetId > 0 => new TouristQrNavigationTarget(qrCode.Code, TouristQrTargetKind.Poi, $"poi-{qrCode.TargetId}"),
            "tour" when qrCode.TargetId > 0 => new TouristQrNavigationTarget(qrCode.Code, TouristQrTargetKind.Tour, $"tour-{qrCode.TargetId}"),
            "open_app" => new TouristQrNavigationTarget(qrCode.Code, TouristQrTargetKind.OpenApp, null),
            _ => new TouristQrNavigationTarget(qrCode.Code, TouristQrTargetKind.OpenApp, null)
        };
    }
}

public static class TouristQrDeepLinkParser
{
    public static bool TryParse(string? rawUri, out TouristQrDeepLinkRequest? request)
    {
        request = null;

        if (string.IsNullOrWhiteSpace(rawUri) || !Uri.TryCreate(rawUri, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return TryParse(uri, out request);
    }

    public static bool TryParse(Uri? uri, out TouristQrDeepLinkRequest? request)
    {
        request = null;
        var code = ExtractCode(uri);
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        request = new TouristQrDeepLinkRequest(Uri.UnescapeDataString(code), uri!.AbsoluteUri);
        return true;
    }

    private static string? ExtractCode(Uri? uri)
    {
        if (uri is null)
        {
            return null;
        }

        if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && uri.Host.Equals("narration.app", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractHostedCode(uri);
        }

        if (uri.Scheme.Equals("foodstreet", StringComparison.OrdinalIgnoreCase)
            && uri.Host.Equals("qr", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractCustomSchemeCode(uri);
        }

        return null;
    }

    private static string? ExtractHostedCode(Uri uri)
    {
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments is ["qr", { Length: > 0 } code] ? code : null;
    }

    private static string? ExtractCustomSchemeCode(Uri uri)
    {
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments is [{ Length: > 0 } code] ? code : null;
    }
}
