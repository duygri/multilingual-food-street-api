using System.Net;
using System.Text;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Poi;

namespace NarrationApp.Server.Services;

internal static partial class QrLaunchHtmlBuilder
{
    private static string BuildPoiListCardsHtml(
        IReadOnlyList<PoiDto> pois,
        IReadOnlyDictionary<int, IReadOnlyList<AudioDto>> audioItemsByPoiId)
    {
        if (pois.Count == 0)
        {
            return """
                <article class="poi-card" data-poi-card>
                    <div class="poi-card__body">
                        <h2>Chưa có POI published</h2>
                        <p>Danh sách sẽ tự hiển thị khi admin publish POI trong hệ thống.</p>
                    </div>
                </article>
                """;
        }

        var builder = new StringBuilder();
        foreach (var poi in pois)
        {
            audioItemsByPoiId.TryGetValue(poi.Id, out var audioItems);
            builder.Append(BuildPoiListCardHtml(poi, audioItems ?? Array.Empty<AudioDto>()));
        }

        return builder.ToString();
    }

    private static string BuildPoiListCardHtml(PoiDto poi, IReadOnlyList<AudioDto> audioItems)
    {
        var primaryTranslation = ResolvePrimaryTranslation(poi);
        var readyAudio = SelectReadyAudio(audioItems);
        var defaultAudio = readyAudio.FirstOrDefault();

        var safeTitle = WebUtility.HtmlEncode(primaryTranslation?.Title ?? poi.Name);
        var safeCategory = WebUtility.HtmlEncode(poi.CategoryName ?? "POI");
        var safeDescription = WebUtility.HtmlEncode(primaryTranslation?.Description ?? poi.Description);
        var safeImageUrl = WebUtility.HtmlEncode(poi.ImageUrl ?? string.Empty);
        var safeDefaultAudioUrl = WebUtility.HtmlEncode(defaultAudio?.Url ?? string.Empty);
        var safeDefaultAudioLabel = WebUtility.HtmlEncode(defaultAudio is null ? "Chưa có audio sẵn sàng" : BuildAudioLabel(defaultAudio));
        var imageBlock = string.IsNullOrWhiteSpace(poi.ImageUrl)
            ? string.Empty
            : $"""<img class="poi-card__image" src="{safeImageUrl}" alt="{safeTitle}">""";
        var audioBlock = defaultAudio is null
            ? """<div class="audio-box empty-audio">POI này chưa có audio public sẵn sàng.</div>"""
            : $"""
                <div class="audio-box">
                    <div class="audio-toolbar">
                        {BuildAudioButtonsHtml(readyAudio)}
                    </div>
                    <strong data-audio-label>{safeDefaultAudioLabel}</strong>
                    <audio controls preload="metadata" src="{safeDefaultAudioUrl}"></audio>
                </div>
                """;

        return $"""
            <article class="poi-card" data-poi-card>
                {imageBlock}
                <div class="poi-card__body">
                    <p class="eyebrow">POI #{poi.Id}</p>
                    <h2>{safeTitle}</h2>
                    <p>{safeDescription}</p>
                    <div class="poi-meta">
                        <span class="pill">{safeCategory}</span>
                        <span class="pill">{readyAudio.Count} ngôn ngữ</span>
                    </div>
                    {audioBlock}
                </div>
            </article>
            """;
    }
}
