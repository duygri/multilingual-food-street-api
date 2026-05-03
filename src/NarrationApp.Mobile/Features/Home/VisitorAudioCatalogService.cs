using System.Net.Http.Json;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Mobile.Features.Home;

public interface IVisitorAudioCatalogService
{
    Task<VisitorAudioCue> LoadBestForPoiAsync(
        string poiId,
        string preferredLanguageCode,
        string? poiName = null,
        CancellationToken cancellationToken = default);
}

public enum VisitorAudioPlaybackState
{
    Idle,
    Loading,
    Ready,
    Playing,
    Paused,
    Error
}

public sealed class VisitorAudioCatalogService(
    HttpClient httpClient,
    IVisitorOfflineCacheStore offlineCacheStore) : IVisitorAudioCatalogService
{
    public async Task<VisitorAudioCue> LoadBestForPoiAsync(
        string poiId,
        string preferredLanguageCode,
        string? poiName = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseServerPoiId(poiId, out var serverPoiId))
        {
            return VisitorAudioCue.Unavailable(poiId, "Audio demo chưa gắn asset thật.");
        }

        var cachedAudio = await FindCachedAudioAsync(poiId, preferredLanguageCode, cancellationToken);
        if (cachedAudio is not null)
        {
            return ToAudioCue(cachedAudio, preferredLanguageCode);
        }

        try
        {
            var response = await httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<AudioDto>>>(
                $"api/audio?poiId={serverPoiId}",
                cancellationToken);

            var assets = response?.Data ?? [];
            var readyAssets = assets
                .Where(asset => asset.Status == AudioStatus.Ready)
                .OrderBy(asset => asset.LanguageCode.Equals(preferredLanguageCode, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(asset => asset.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(asset => asset.SourceType == AudioSourceType.Recorded ? 0 : 1)
                .ToList();

            var selected = readyAssets.FirstOrDefault();
            if (selected is null)
            {
                return VisitorAudioCue.Unavailable(poiId, "Chưa có audio sẵn sàng cho POI này.");
            }

            var streamUrl = ToAbsoluteUrl(selected.Url);
            var cachedEntry = await CacheSelectedAudioAsync(poiId, poiName, selected, streamUrl, preferredLanguageCode, cancellationToken);
            if (cachedEntry is not null)
            {
                return ToAudioCue(cachedEntry, preferredLanguageCode);
            }

            return new VisitorAudioCue(
                PoiId: poiId,
                LanguageCode: selected.LanguageCode,
                StreamUrl: streamUrl,
                DurationSeconds: selected.DurationSeconds,
                IsAvailable: true,
                StatusLabel: BuildStatusLabel(selected, preferredLanguageCode),
                IsPreferredLanguage: selected.LanguageCode.Equals(preferredLanguageCode, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            return VisitorAudioCue.Unavailable(poiId, $"Không tải được audio: {ex.Message}");
        }
    }

    private async Task<VisitorAudioCacheEntry?> FindCachedAudioAsync(
        string poiId,
        string preferredLanguageCode,
        CancellationToken cancellationToken)
    {
        try
        {
            return await offlineCacheStore.FindBestAudioAsync(poiId, preferredLanguageCode, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task<VisitorAudioCacheEntry?> CacheSelectedAudioAsync(
        string poiId,
        string? poiName,
        AudioDto selected,
        string streamUrl,
        string preferredLanguageCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var audioStream = await httpClient.GetStreamAsync(streamUrl, cancellationToken);
            return await offlineCacheStore.CacheAudioAsync(
                new VisitorAudioCacheRequest(
                    PoiId: poiId,
                    PoiName: string.IsNullOrWhiteSpace(poiName) ? poiId : poiName,
                    LanguageCode: selected.LanguageCode,
                    SourceUrl: streamUrl,
                    SourceLabel: selected.SourceType == AudioSourceType.Recorded ? "Recorded" : "Google TTS",
                    StatusLabel: BuildOfflineStatusLabel(selected, preferredLanguageCode),
                    DurationSeconds: selected.DurationSeconds),
                audioStream,
                cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static VisitorAudioCue ToAudioCue(VisitorAudioCacheEntry entry, string preferredLanguageCode)
    {
        return new VisitorAudioCue(
            PoiId: entry.PoiId,
            LanguageCode: entry.LanguageCode,
            StreamUrl: ToLocalPlaybackUrl(entry.LocalFilePath),
            DurationSeconds: entry.DurationSeconds,
            IsAvailable: true,
            StatusLabel: entry.StatusLabel,
            IsPreferredLanguage: entry.LanguageCode.Equals(preferredLanguageCode, StringComparison.OrdinalIgnoreCase));
    }

    private string ToAbsoluteUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        return new Uri(httpClient.BaseAddress!, url.TrimStart('/')).ToString();
    }

    private static string ToLocalPlaybackUrl(string localFilePath)
    {
        return Uri.TryCreate(localFilePath, UriKind.Absolute, out var uri)
            ? uri.AbsoluteUri
            : new Uri(Path.GetFullPath(localFilePath)).AbsoluteUri;
    }

    private static string BuildStatusLabel(AudioDto asset, string preferredLanguageCode)
    {
        var languageLabel = asset.LanguageCode.Equals(preferredLanguageCode, StringComparison.OrdinalIgnoreCase)
            ? preferredLanguageCode.ToUpperInvariant()
            : asset.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase)
                ? "Tiếng Việt"
                : asset.LanguageCode.ToUpperInvariant();

        return asset.SourceType switch
        {
            AudioSourceType.Recorded => $"Sẵn sàng phát • {languageLabel} • ghi âm",
            _ => $"Sẵn sàng phát • {languageLabel} • TTS"
        };
    }

    private static string BuildOfflineStatusLabel(AudioDto asset, string preferredLanguageCode)
    {
        var liveStatus = BuildStatusLabel(asset, preferredLanguageCode);
        return liveStatus.Replace("Sẵn sàng phát", "Sẵn sàng phát offline", StringComparison.Ordinal);
    }

    private static bool TryParseServerPoiId(string poiId, out int serverPoiId)
    {
        serverPoiId = 0;
        return poiId.StartsWith("poi-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(poiId["poi-".Length..], out serverPoiId);
    }
}

public sealed record VisitorAudioCue(
    string PoiId,
    string LanguageCode,
    string StreamUrl,
    int DurationSeconds,
    bool IsAvailable,
    string StatusLabel,
    bool IsPreferredLanguage)
{
    public static VisitorAudioCue Unavailable(string poiId, string statusLabel)
    {
        return new VisitorAudioCue(
            PoiId: poiId,
            LanguageCode: string.Empty,
            StreamUrl: string.Empty,
            DurationSeconds: 0,
            IsAvailable: false,
            StatusLabel: statusLabel,
            IsPreferredLanguage: false);
    }
}
