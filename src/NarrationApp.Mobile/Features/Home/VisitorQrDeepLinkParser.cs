using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Mobile.Features.Home;

public enum VisitorQrTargetKind
{
    OpenApp,
    Poi
}

public sealed record VisitorQrDeepLinkRequest(string Code, string SourceUri);

public sealed record VisitorQrNavigationTarget(string Code, VisitorQrTargetKind Kind, string? TargetId)
{
    public static VisitorQrNavigationTarget FromQrCode(QrCodeDto qrCode)
    {
        var targetType = qrCode.TargetType.Trim().ToLowerInvariant();

        return targetType switch
        {
            "poi" when qrCode.TargetId > 0 => new VisitorQrNavigationTarget(qrCode.Code, VisitorQrTargetKind.Poi, $"poi-{qrCode.TargetId}"),
            "open_app" => new VisitorQrNavigationTarget(qrCode.Code, VisitorQrTargetKind.OpenApp, null),
            _ => new VisitorQrNavigationTarget(qrCode.Code, VisitorQrTargetKind.OpenApp, null)
        };
    }
}

public static class VisitorQrDeepLinkParser
{
    public static bool TryParse(string? rawUri, out VisitorQrDeepLinkRequest? request)
    {
        request = null;

        if (string.IsNullOrWhiteSpace(rawUri) || !Uri.TryCreate(rawUri, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return TryParse(uri, out request);
    }

    public static bool TryParse(Uri? uri, out VisitorQrDeepLinkRequest? request)
    {
        request = null;
        var code = ExtractCode(uri);
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        request = new VisitorQrDeepLinkRequest(Uri.UnescapeDataString(code), uri!.AbsoluteUri);
        return true;
    }

    private static string? ExtractCode(Uri? uri)
    {
        if (uri is null)
        {
            return null;
        }

        if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
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
