using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private string GetCachePreloadActionLabel() =>
        UiText.FormatPreloadAudioLabel(UiText.LocalizeLanguageLabel(_state.CurrentLanguage.Label));

    private async Task PreloadSelectedLanguageAudioAsync()
    {
        if (_isCachePreloadRunning)
        {
            return;
        }

        _isCachePreloadRunning = true;
        _cachePreloadProgressPercent = 0;
        _cachePreloadStatusLabel = UiText.FormatPreparingCacheStatus(UiText.LocalizeLanguageLabel(_state.CurrentLanguage.Label));

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
            _cachePreloadStatusLabel = UiText.FormatPreloadFailure(ex.Message);
        }
        finally
        {
            _isCachePreloadRunning = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void UpdateCachePreloadProgress(VisitorAudioPreloadProgress progress)
    {
        _cachePreloadStatusLabel = UiText.LocalizeKnownStatus(progress.StatusLabel);
        _cachePreloadProgressPercent = progress.Total == 0
            ? 100d
            : Math.Clamp(progress.Completed * 100d / progress.Total, 0d, 100d);
        _ = InvokeAsync(StateHasChanged);
    }

    private string BuildCachePreloadResultLabel(VisitorAudioPreloadResult result) =>
        UiText.FormatPreloadResult(result);
}
