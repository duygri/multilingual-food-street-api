using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Web.Pages.Admin;

public partial class QrManagement
{
    private void ShowComposer()
    {
        _showComposer = true;
        EnsureQrDefaults();
    }

    private void CloseComposer() => _showComposer = false;

    private void ViewQr(QrCodeDto qr) => _previewQr = qr;

    private void ClosePreview() => _previewQr = null;

    private void EnsureQrDefaults()
    {
        if (_poiOptions.Count > 0 && _qrEditor.PoiId <= 0)
        {
            _qrEditor.PoiId = _poiOptions[0].Id;
        }
    }

    private void UpdateQrTargetType(string? value)
    {
        _qrEditor.TargetType = string.IsNullOrWhiteSpace(value) ? "poi" : value;
        EnsureQrDefaults();
    }

    private void UpdateQrPoi(string? value)
    {
        if (int.TryParse(value, out var poiId))
        {
            _qrEditor.PoiId = poiId;
        }
    }
}
