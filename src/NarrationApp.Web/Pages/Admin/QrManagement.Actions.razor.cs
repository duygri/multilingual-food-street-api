using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class QrManagement
{
    private async Task LoadQrItemsAsync()
    {
        _qrItems = await QrPortalService.GetAsync(string.IsNullOrWhiteSpace(_qrFilter) ? null : _qrFilter);
        EnsureQrDefaults();
    }

    private async Task ChangeQrFilterAsync(string? value)
    {
        _qrFilter = value ?? string.Empty;
        await LoadQrItemsAsync();
    }

    private async Task CreateQrAsync()
    {
        try
        {
            var created = await QrPortalService.CreateAsync(_qrEditor.ToRequest());
            _statusMessage = $"Đã tạo QR mới: {created.Code}.";
            await LoadQrItemsAsync();
            _showComposer = false;
            _previewQr = created;
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }

    private async Task DeleteQrAsync(int qrId)
    {
        try
        {
            await QrPortalService.DeleteAsync(qrId);
            _statusMessage = "Đã xóa QR khỏi hệ thống.";
            await LoadQrItemsAsync();
            if (_previewQr?.Id == qrId)
            {
                _previewQr = null;
            }
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }
}
