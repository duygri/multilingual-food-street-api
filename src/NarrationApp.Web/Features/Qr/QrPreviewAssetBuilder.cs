using System.Text;

namespace NarrationApp.Web.Features.Qr;

public static class QrPreviewAssetBuilder
{
    public static string BuildPublicUrl(string baseUri, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var rootUri = new Uri(baseUri, UriKind.Absolute);
        var relativePath = $"qr/{Uri.EscapeDataString(code.Trim())}";
        return new Uri(rootUri, relativePath).ToString();
    }

    public static string BuildImageDataUri(string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        using var generator = new QRCoder.QRCodeGenerator();
        using var qrData = generator.CreateQrCode(payload, QRCoder.QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCoder.PngByteQRCode(qrData);
        var pngBytes = qrCode.GetGraphic(20, drawQuietZones: true);
        var encoded = Convert.ToBase64String(pngBytes);
        return $"data:image/png;base64,{encoded}";
    }
}
