namespace NarrationApp.Mobile.Features.Home;

public interface IVisitorAudioPreloadService
{
    Task<VisitorAudioPreloadResult> PreloadAsync(
        IReadOnlyList<VisitorPoi> pois,
        string languageCode,
        IProgress<VisitorAudioPreloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record VisitorAudioPreloadProgress(
    int Completed,
    int Total,
    string StatusLabel);

public sealed record VisitorAudioPreloadResult(
    int Total,
    int Downloaded,
    int Skipped,
    int Failed);

public sealed class VisitorAudioPreloadService(
    IVisitorAudioCatalogService audioCatalogService,
    IVisitorOfflineCacheStore offlineCacheStore) : IVisitorAudioPreloadService
{
    public async Task<VisitorAudioPreloadResult> PreloadAsync(
        IReadOnlyList<VisitorPoi> pois,
        string languageCode,
        IProgress<VisitorAudioPreloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedLanguageCode = languageCode.Trim();
        var candidates = pois
            .Where(poi => HasReadyAudio(poi, normalizedLanguageCode))
            .OrderBy(poi => poi.DistanceMeters)
            .ThenByDescending(poi => poi.Priority)
            .ToArray();

        var cachedKeys = await LoadCachedAudioKeysAsync(cancellationToken);
        var remaining = candidates
            .Where(poi => !cachedKeys.Contains(CreateCacheKey(poi.Id, normalizedLanguageCode)))
            .ToArray();

        var skipped = candidates.Length - remaining.Length;
        var downloaded = 0;
        var failed = 0;

        progress?.Report(new VisitorAudioPreloadProgress(
            Completed: 0,
            Total: remaining.Length,
            StatusLabel: BuildProgressLabel(0, remaining.Length, skipped)));

        foreach (var poi in remaining)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cue = await audioCatalogService.LoadBestForPoiAsync(
                poi.Id,
                normalizedLanguageCode,
                poi.Name,
                cancellationToken);

            if (cue.IsAvailable && IsLocalPlaybackUrl(cue.StreamUrl))
            {
                downloaded++;
            }
            else
            {
                failed++;
            }

            progress?.Report(new VisitorAudioPreloadProgress(
                Completed: downloaded + failed,
                Total: remaining.Length,
                StatusLabel: BuildProgressLabel(downloaded + failed, remaining.Length, skipped)));
        }

        return new VisitorAudioPreloadResult(
            Total: candidates.Length,
            Downloaded: downloaded,
            Skipped: skipped,
            Failed: failed);
    }

    private async Task<HashSet<string>> LoadCachedAudioKeysAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cachedItems = await offlineCacheStore.ListCachedAudioAsync(cancellationToken);
            return cachedItems
                .Select(item => CreateCacheKey(item.PoiId, item.LanguageCode))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return [];
        }
    }

    private static bool HasReadyAudio(VisitorPoi poi, string languageCode)
    {
        return poi.ReadyAudioLanguageCodes.Any(readyLanguageCode =>
            string.Equals(readyLanguageCode, languageCode, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLocalPlaybackUrl(string streamUrl)
    {
        return Uri.TryCreate(streamUrl, UriKind.Absolute, out var uri)
            && string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateCacheKey(string poiId, string languageCode) =>
        $"{poiId}|{languageCode}".ToLowerInvariant();

    private static string BuildProgressLabel(int completed, int total, int skipped)
    {
        if (total == 0)
        {
            return skipped > 0
                ? $"Đã có sẵn {skipped} file trong cache."
                : "Không có audio phù hợp để tải trước.";
        }

        return skipped > 0
            ? $"Đang tải {completed}/{total}. Bỏ qua {skipped} file đã có."
            : $"Đang tải {completed}/{total}.";
    }
}
