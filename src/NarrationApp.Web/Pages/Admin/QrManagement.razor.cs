using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.QR;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class QrManagement
{
    private bool _isLoading = true;
    private bool _showComposer;
    private string? _errorMessage;
    private string? _statusMessage;
    private IReadOnlyList<PoiDto> _poiOptions = Array.Empty<PoiDto>();
    private IReadOnlyList<QrCodeDto> _qrItems = Array.Empty<QrCodeDto>();
    private QrEditorModel _qrEditor = QrEditorModel.CreateDefault();
    private QrCodeDto? _previewQr;
    private string _qrFilter = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _poiOptions = await TourPortalService.GetPoiOptionsAsync();
            await LoadQrItemsAsync();
        }
        catch (ApiException exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
