using NarrationApp.Shared.DTOs.QR;
using NarrationApp.SharedUI.Models;
using NarrationApp.Web.Features.Qr;
using System.Net;

namespace NarrationApp.Web.Pages.Admin;

public partial class QrManagement
{
    private string BuildPreviewPublicUrl(QrCodeDto qr) =>
        !string.IsNullOrWhiteSpace(qr.PublicUrl)
            ? qr.PublicUrl
            : QrPreviewAssetBuilder.BuildPublicUrl(QrPublicUrlOptions.BaseAddress.ToString(), qr.Code);

    private string BuildPreviewQrImageSource(QrCodeDto qr) => QrPreviewAssetBuilder.BuildImageDataUri(BuildPreviewPublicUrl(qr));

    private bool ShouldWarnPreviewUrlIsNotShareable(QrCodeDto qr)
    {
        var previewPublicUrl = BuildPreviewPublicUrl(qr);
        return Uri.TryCreate(previewPublicUrl, UriKind.Absolute, out var previewUri)
            && (string.Equals(previewUri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                || (IPAddress.TryParse(previewUri.Host, out var hostAddress) && IPAddress.IsLoopback(hostAddress)));
    }

    private int CountByTarget(string targetType) =>
        _qrItems.Count(item => string.Equals(item.TargetType, targetType, StringComparison.OrdinalIgnoreCase));

    private string GetQrTargetLabel(QrCodeDto qr) => qr.TargetType switch
    {
        "poi" => _poiOptions.FirstOrDefault(item => item.Id == qr.TargetId)?.Name ?? $"POI #{qr.TargetId}",
        "open_app" => "Mở ứng dụng chính",
        _ => $"Target #{qr.TargetId}"
    };

    private static string GetScanCountLabel(QrCodeDto qr) => qr.ScanCount?.ToString("N0") ?? "Chưa track";
    private static string GetQrTypeLabel(string targetType) => targetType switch { "open_app" => "Open App", "poi" => "POI", _ => targetType };
    private static StatusTone GetQrTone(string targetType) => targetType switch { "open_app" => StatusTone.Neutral, "poi" => StatusTone.Good, _ => StatusTone.Neutral };
}
