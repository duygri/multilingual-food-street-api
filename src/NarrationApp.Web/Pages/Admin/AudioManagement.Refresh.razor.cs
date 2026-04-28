using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class AudioManagement
{
    private async Task RefreshAllAsync()
    {
        _statusMessage = null;
        await LoadDashboardAsync();
    }

    private async Task HandleAutoRefreshTickAsync(CancellationToken cancellationToken)
    {
        if (_isLoading || _isGenerating || _isUploading || _isAutoRefreshing || !HasProcessingAudio)
        {
            return;
        }

        await InvokeAsync(() => RefreshProcessingAudioAsync(cancellationToken));
    }

    private async Task RefreshProcessingAudioAsync(CancellationToken cancellationToken = default)
    {
        var processingPoiIds = _audioByPoi
            .Where(entry => entry.Value.Any(item => item.Status is AudioStatus.Requested or AudioStatus.Generating))
            .Select(entry => entry.Key)
            .ToArray();

        if (processingPoiIds.Length == 0)
        {
            return;
        }

        _isAutoRefreshing = true;

        try
        {
            foreach (var poiId in processingPoiIds)
            {
                _audioByPoi[poiId] = await AudioPortalService.GetByPoiAsync(poiId, cancellationToken: cancellationToken);
            }
        }
        catch (ApiException)
        {
        }
        finally
        {
            _isAutoRefreshing = false;
            StateHasChanged();
        }
    }

    private async Task RefreshPoiAudioAsync(AdminPoiDto poi)
    {
        try
        {
            _audioByPoi[poi.Id] = await AudioPortalService.GetByPoiAsync(poi.Id);
            _selectedPoi = poi;
            _statusMessage = $"Đã làm mới audio của {poi.Name}.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }
}
