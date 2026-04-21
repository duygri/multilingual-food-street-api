using System.Net.Http.Json;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Mobile.Features.Home;

public interface ITouristAudioCatalogService
{
    Task<TouristAudioCue> LoadBestForPoiAsync(string poiId, string preferredLanguageCode, CancellationToken cancellationToken = default);
}

public enum TouristAudioPlaybackState
{
    Idle,
    Loading,
    Ready,
    Playing,
    Paused,
    Error
}

public sealed class TouristAudioCatalogService(HttpClient httpClient) : ITouristAudioCatalogService
{
    public async Task<TouristAudioCue> LoadBestForPoiAsync(string poiId, string preferredLanguageCode, CancellationToken cancellationToken = default)
    {
        if (!TryParseServerPoiId(poiId, out var serverPoiId))
        {
            return TouristAudioCue.Unavailable(poiId, "Audio demo chưa gắn asset thật.");
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
                return TouristAudioCue.Unavailable(poiId, "Chưa có audio sẵn sàng cho POI này.");
            }

            return new TouristAudioCue(
                PoiId: poiId,
                LanguageCode: selected.LanguageCode,
                StreamUrl: ToAbsoluteUrl(selected.Url),
                DurationSeconds: selected.DurationSeconds,
                IsAvailable: true,
                StatusLabel: BuildStatusLabel(selected, preferredLanguageCode),
                IsPreferredLanguage: selected.LanguageCode.Equals(preferredLanguageCode, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            return TouristAudioCue.Unavailable(poiId, $"Không tải được audio: {ex.Message}");
        }
    }

    private string ToAbsoluteUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        return new Uri(httpClient.BaseAddress!, url.TrimStart('/')).ToString();
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

    private static bool TryParseServerPoiId(string poiId, out int serverPoiId)
    {
        serverPoiId = 0;
        return poiId.StartsWith("poi-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(poiId["poi-".Length..], out serverPoiId);
    }
}

public sealed record TouristAudioCue(
    string PoiId,
    string LanguageCode,
    string StreamUrl,
    int DurationSeconds,
    bool IsAvailable,
    string StatusLabel,
    bool IsPreferredLanguage)
{
    public static TouristAudioCue Unavailable(string poiId, string statusLabel)
    {
        return new TouristAudioCue(
            PoiId: poiId,
            LanguageCode: string.Empty,
            StreamUrl: string.Empty,
            DurationSeconds: 0,
            IsAvailable: false,
            StatusLabel: statusLabel,
            IsPreferredLanguage: false);
    }
}
