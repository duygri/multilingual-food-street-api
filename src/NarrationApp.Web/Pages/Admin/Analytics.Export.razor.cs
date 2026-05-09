using Microsoft.JSInterop;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class Analytics
{
    private bool _isExportingEventLog;
    private string? _eventLogExportMessage;
    private string? _eventLogExportErrorMessage;

    private async Task DownloadEventLogCsvAsync()
    {
        _isExportingEventLog = true;
        _eventLogExportMessage = null;
        _eventLogExportErrorMessage = null;

        try
        {
            var export = await AdminPortalService.ExportEventLogCsvAsync();
            await JsRuntime.InvokeVoidAsync(
                "portalDownloads.saveBase64",
                export.FileName,
                export.ContentType,
                Convert.ToBase64String(export.Content));
            _eventLogExportMessage = $"Đã xuất {export.FileName}";
        }
        catch (ApiException exception)
        {
            _eventLogExportErrorMessage = exception.Message;
        }
        finally
        {
            _isExportingEventLog = false;
        }
    }
}
