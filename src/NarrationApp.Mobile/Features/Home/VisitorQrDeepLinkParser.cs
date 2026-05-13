using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Mobile.Features.Home;

public enum VisitorQrTargetKind
{
    OpenApp,
    PoiList,
    Poi,
    Tour
}

public sealed record VisitorQrDeepLinkRequest(string Code, string SourceUri)
{
    public string? QrLaunchMode { get; init; }
}

public sealed record VisitorQrNavigationTarget(string Code, VisitorQrTargetKind Kind, string? TargetId)
{
    public static VisitorQrNavigationTarget FromQrCode(QrCodeDto qrCode)
    {
        var targetType = qrCode.TargetType.Trim().ToLowerInvariant();

        return targetType switch
        {
            "poi" when qrCode.TargetId > 0 => new VisitorQrNavigationTarget(qrCode.Code, VisitorQrTargetKind.Poi, $"poi-{qrCode.TargetId}"),
            "tour" when qrCode.TargetId > 0 => new VisitorQrNavigationTarget(qrCode.Code, VisitorQrTargetKind.Tour, $"tour-{qrCode.TargetId}"),
            "poi_list" => new VisitorQrNavigationTarget(qrCode.Code, VisitorQrTargetKind.PoiList, null),
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

        request = new VisitorQrDeepLinkRequest(Uri.UnescapeDataString(code), uri!.AbsoluteUri)
        {
            QrLaunchMode = ExtractQueryValue(uri, "qrLaunch")
        };
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

        if (uri.Scheme.Equals("foodstreet", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractCustomSchemeCode(uri);
        }

        return null;
    }

    private static string? ExtractHostedCode(Uri uri)
    {
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments is ["qr", { Length: > 0 } code])
        {
            return code;
        }

        return segments is ["qr"] ? ExtractQueryCode(uri) : null;
    }

    private static string? ExtractCustomSchemeCode(Uri uri)
    {
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (uri.Host.Equals("qr", StringComparison.OrdinalIgnoreCase))
        {
            return segments is [{ Length: > 0 } code] ? code : ExtractQueryCode(uri);
        }

        if (segments is ["qr", { Length: > 0 } hostedPathCode])
        {
            return hostedPathCode;
        }

        return segments is ["qr"] ? ExtractQueryCode(uri) : null;
    }

    private static string? ExtractQueryCode(Uri uri)
    {
        if (string.IsNullOrWhiteSpace(uri.Query))
        {
            return null;
        }

        return ExtractQueryValue(uri, "code");
    }

    private static string? ExtractQueryValue(Uri uri, string key)
    {
        if (string.IsNullOrWhiteSpace(uri.Query))
        {
            return null;
        }

        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var name = Uri.UnescapeDataString(parts[0]).Replace("+", " ", StringComparison.Ordinal);
            if (!name.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = parts.Length == 2 ? parts[1] : string.Empty;
            return Uri.UnescapeDataString(value).Replace("+", " ", StringComparison.Ordinal);
        }

        return null;
    }
}
