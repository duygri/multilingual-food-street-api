using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private string GetCachePreloadActionLabel() =>
        $"Tải trước {_state.CurrentLanguage.Label}";

    private async Task PreloadSelectedLanguageAudioAsync()
    {
        if (_isCachePreloadRunning)
        {
            return;
        }

        _isCachePreloadRunning = true;
        _cachePreloadProgressPercent = 0;
        _cachePreloadStatusLabel = $"Đang chuẩn bị tải audio {_state.CurrentLanguage.Label}...";

        try
        {
            var progress = new Progress<VisitorAudioPreloadProgress>(UpdateCachePreloadProgress);
            var result = await AudioPreloadService.PreloadAsync(_state.Pois, _state.SelectedLanguageCode, progress);
            await RefreshCachedAudioItemsAsync();
            _cachePreloadProgressPercent = result.Total == 0 ? 0 : 100d;
            _cachePreloadStatusLabel = BuildCachePreloadResultLabel(result);
        }
        catch (Exception ex)
        {
            _cachePreloadStatusLabel = $"Không tải trước được audio: {ex.Message}";
        }
        finally
        {
            _isCachePreloadRunning = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void UpdateCachePreloadProgress(VisitorAudioPreloadProgress progress)
    {
        _cachePreloadStatusLabel = progress.StatusLabel;
        _cachePreloadProgressPercent = progress.Total == 0
            ? 100d
            : Math.Clamp(progress.Completed * 100d / progress.Total, 0d, 100d);
        _ = InvokeAsync(StateHasChanged);
    }

    private static string BuildCachePreloadResultLabel(VisitorAudioPreloadResult result)
    {
        if (result.Total == 0)
        {
            return "Không có POI nào có audio sẵn cho ngôn ngữ hiện tại.";
        }

        return result.Failed > 0
            ? $"Đã tải {result.Downloaded}, bỏ qua {result.Skipped}, lỗi {result.Failed}."
            : $"Đã sẵn sàng offline: tải mới {result.Downloaded}, đã có {result.Skipped}.";
    }
}
