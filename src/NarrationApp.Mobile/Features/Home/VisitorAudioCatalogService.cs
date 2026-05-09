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
        var normalizedPreferredLanguageCode = NormalizeLanguageCode(preferredLanguageCode);
        if (!TryParseServerPoiId(poiId, out var serverPoiId))
        {
            return VisitorAudioCue.Unavailable(poiId, "Audio demo chưa gắn asset thật.");
        }

        try
        {
            return await LoadLiveCueAsync(poiId, serverPoiId, normalizedPreferredLanguageCode, poiName, cancellationToken);
        }
        catch (Exception ex)
        {
            return await LoadCachedCueOrUnavailableAsync(poiId, normalizedPreferredLanguageCode, ex, cancellationToken);
        }
    }

    private async Task<VisitorAudioCue> LoadLiveCueAsync(
        string poiId,
        int serverPoiId,
        string preferredLanguageCode,
        string? poiName,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<AudioDto>>>(
            $"api/audio?poiId={serverPoiId}",
            cancellationToken);

        var selected = SelectReadyAsset(response?.Data ?? [], preferredLanguageCode);
        if (selected is null)
        {
            return VisitorAudioCue.Unavailable(poiId, BuildUnavailableStatusLabel(preferredLanguageCode));
        }

        return await BuildCueFromReadyAssetAsync(poiId, poiName, selected, preferredLanguageCode, cancellationToken);
    }

    private async Task<VisitorAudioCue> BuildCueFromReadyAssetAsync(
        string poiId,
        string? poiName,
        AudioDto selected,
        string preferredLanguageCode,
        CancellationToken cancellationToken)
    {
        var streamUrl = ToAbsoluteUrl(selected.Url);
        var cachedEntry = await CacheSelectedAudioAsync(poiId, poiName, selected, streamUrl, preferredLanguageCode, cancellationToken);
        return cachedEntry is not null
            ? ToAudioCue(cachedEntry, preferredLanguageCode)
            : ToNetworkAudioCue(poiId, selected, streamUrl, preferredLanguageCode);
    }

    private async Task<VisitorAudioCue> LoadCachedCueOrUnavailableAsync(
        string poiId,
        string preferredLanguageCode,
        Exception liveLoadException,
        CancellationToken cancellationToken)
    {
        var cachedAudio = await FindCachedAudioAsync(poiId, preferredLanguageCode, cancellationToken);
        return cachedAudio is not null
            ? ToAudioCue(cachedAudio, preferredLanguageCode)
            : VisitorAudioCue.Unavailable(poiId, $"Không tải được audio: {liveLoadException.Message}");
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

    private static VisitorAudioCue ToNetworkAudioCue(
        string poiId,
        AudioDto selected,
        string streamUrl,
        string preferredLanguageCode)
    {
        return new VisitorAudioCue(
            PoiId: poiId,
            LanguageCode: selected.LanguageCode,
            StreamUrl: streamUrl,
            DurationSeconds: selected.DurationSeconds,
            IsAvailable: true,
            StatusLabel: BuildStatusLabel(selected, preferredLanguageCode),
            IsPreferredLanguage: selected.LanguageCode.Equals(preferredLanguageCode, StringComparison.OrdinalIgnoreCase));
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

    private static string BuildUnavailableStatusLabel(string preferredLanguageCode)
    {
        return $"Chưa có audio sẵn sàng cho ngôn ngữ {preferredLanguageCode.ToUpperInvariant()}.";
    }

    private static AudioDto? SelectReadyAsset(IReadOnlyList<AudioDto> assets, string preferredLanguageCode)
    {
        return assets
            .Where(asset => asset.Status == AudioStatus.Ready)
            .Where(asset => asset.LanguageCode.Equals(preferredLanguageCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(asset => asset.SourceType == AudioSourceType.Recorded ? 0 : 1)
            .FirstOrDefault();
    }

    private static string NormalizeLanguageCode(string languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "vi"
            : languageCode.Trim().ToLowerInvariant();
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
