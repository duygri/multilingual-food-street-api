using System.Net;
using System.Text;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

internal static class SharePreviewHtmlBuilder
{
    public static string BuildPoiShareHtml(PoiDto poi, IReadOnlyList<AudioDto> audioItems, string publicUrl)
    {
        var primaryTranslation = ResolvePrimaryTranslation(poi);
        var readyAudio = audioItems
            .Where(item => item.Status == AudioStatus.Ready && !string.IsNullOrWhiteSpace(item.Url))
            .OrderByDescending(item => string.Equals(item.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
            .ThenBy(item => item.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var defaultAudio = readyAudio.FirstOrDefault();

        var safeTitle = Html(primaryTranslation?.Title ?? poi.Name);
        var safeDescription = Html(primaryTranslation?.Description ?? poi.Description);
        var safeStory = Html(primaryTranslation?.Story ?? poi.TtsScript);
        var safeCategory = Html(poi.CategoryName ?? "POI");
        var safePublicUrl = Html(publicUrl);
        var safeImageUrl = Html(poi.ImageUrl ?? string.Empty);
        var safeDefaultAudioUrl = Html(defaultAudio?.Url ?? string.Empty);

        var imageMeta = string.IsNullOrWhiteSpace(poi.ImageUrl)
            ? string.Empty
            : $"""<meta property="og:image" content="{safeImageUrl}">""";

        var mediaBlock = string.IsNullOrWhiteSpace(poi.ImageUrl)
            ? string.Empty
            : $"""<img class="hero-image" src="{safeImageUrl}" alt="{safeTitle}">""";

        var audioBlock = defaultAudio is null
            ? """
              <div class="empty">POI này chưa có audio public sẵn sàng.</div>
              """
            : $"""
              <div class="chips">{BuildAudioButtonsHtml(readyAudio)}</div>
              <audio id="share-audio" controls preload="metadata" src="{safeDefaultAudioUrl}"></audio>
              """;

        return $$"""
<!doctype html>
<html lang="vi">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
    <title>{{safeTitle}} · Food Street</title>
    <meta name="description" content="{{safeDescription}}">
    <meta property="og:type" content="article">
    <meta property="og:title" content="{{safeTitle}}">
    <meta property="og:description" content="{{safeDescription}}">
    <meta property="og:url" content="{{safePublicUrl}}">
    {{imageMeta}}
    {{BuildStyle()}}
</head>
<body>
    <main class="shell">
        <section class="hero">
            {{mediaBlock}}
            <div class="copy">
                <p class="eyebrow">Public share</p>
                <h1>{{safeTitle}}</h1>
                <p>{{safeDescription}}</p>
                <div class="meta-row">
                    <span>{{safeCategory}}</span>
                    <span>POI #{{poi.Id}}</span>
                </div>
            </div>
        </section>
        <section class="panel">
            <p class="eyebrow">Nghe thử</p>
            {{audioBlock}}
        </section>
        <section class="panel">
            <p class="eyebrow">Câu chuyện</p>
            <p class="story">{{safeStory}}</p>
        </section>
        <section class="panel subtle">
            <p class="eyebrow">Link chia sẻ</p>
            <p class="link">{{safePublicUrl}}</p>
        </section>
    </main>
    <script>
        const player = document.getElementById('share-audio');
        document.querySelectorAll('[data-audio-src]').forEach(function (button) {
            button.addEventListener('click', function () {
                if (!player) return;
                document.querySelectorAll('[data-audio-src]').forEach(function (item) {
                    item.classList.remove('active');
                });
                button.classList.add('active');
                player.src = button.getAttribute('data-audio-src') || '';
                player.play().catch(function () {});
            });
        });
    </script>
</body>
</html>
""";
    }

    public static string BuildTourShareHtml(TourDto tour, string publicUrl)
    {
        var safeTitle = Html(tour.Title);
        var safeDescription = Html(tour.Description);
        var safePublicUrl = Html(publicUrl);
        var safeCoverImage = Html(tour.CoverImage ?? string.Empty);
        var imageMeta = string.IsNullOrWhiteSpace(tour.CoverImage)
            ? string.Empty
            : $"""<meta property="og:image" content="{safeCoverImage}">""";
        var mediaBlock = string.IsNullOrWhiteSpace(tour.CoverImage)
            ? string.Empty
            : $"""<img class="hero-image" src="{safeCoverImage}" alt="{safeTitle}">""";

        return $$"""
<!doctype html>
<html lang="vi">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
    <title>{{safeTitle}} · Food Street</title>
    <meta name="description" content="{{safeDescription}}">
    <meta property="og:type" content="article">
    <meta property="og:title" content="{{safeTitle}}">
    <meta property="og:description" content="{{safeDescription}}">
    <meta property="og:url" content="{{safePublicUrl}}">
    {{imageMeta}}
    {{BuildStyle()}}
</head>
<body>
    <main class="shell">
        <section class="hero">
            {{mediaBlock}}
            <div class="copy">
                <p class="eyebrow">Public share</p>
                <h1>{{safeTitle}}</h1>
                <p>{{safeDescription}}</p>
                <div class="meta-row">
                    <span>{{tour.EstimatedMinutes}} phút</span>
                    <span>{{tour.Stops.Count}} điểm dừng</span>
                </div>
            </div>
        </section>
        <section class="panel subtle">
            <p class="eyebrow">Link chia sẻ</p>
            <p class="link">{{safePublicUrl}}</p>
        </section>
    </main>
</body>
</html>
""";
    }

    private static string BuildAudioButtonsHtml(IReadOnlyList<AudioDto> audioItems)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < audioItems.Count; index++)
        {
            var audio = audioItems[index];
            var active = index == 0 ? " active" : string.Empty;
            builder.Append($"""<button class="chip{active}" type="button" data-audio-src="{Html(audio.Url)}">{Html(audio.LanguageCode.ToUpperInvariant())}</button>""");
        }

        return builder.ToString();
    }

    private static string BuildStyle()
    {
        return """
<style>
    :root { color-scheme: dark; }
    * { box-sizing: border-box; }
    body {
        margin: 0;
        min-height: 100vh;
        padding: 24px;
        background: radial-gradient(circle at top, rgba(20,184,166,.18), transparent 34%), #020617;
        color: #e2e8f0;
        font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    }
    .shell { width: min(100%, 860px); margin: 0 auto; display: grid; gap: 18px; }
    .hero, .panel {
        overflow: hidden;
        border-radius: 24px;
        border: 1px solid rgba(45, 212, 191, .18);
        background: rgba(15, 23, 42, .94);
        box-shadow: 0 24px 80px rgba(2, 6, 23, .36);
    }
    .hero-image { width: 100%; max-height: 340px; object-fit: cover; display: block; }
    .copy, .panel { padding: 24px; }
    .eyebrow {
        margin: 0 0 10px;
        color: #2dd4bf;
        text-transform: uppercase;
        letter-spacing: .18em;
        font-size: 12px;
        font-weight: 800;
    }
    h1 { margin: 0 0 12px; font-size: clamp(2rem, 5vw, 3.5rem); line-height: 1.04; }
    p { margin: 0; color: #94a3b8; line-height: 1.65; }
    .meta-row, .chips { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 18px; }
    .meta-row span, .chip {
        display: inline-flex;
        align-items: center;
        min-height: 38px;
        padding: 0 14px;
        border-radius: 999px;
        border: 1px solid rgba(45,212,191,.22);
        background: rgba(8,47,73,.52);
        color: #a5f3fc;
        font-weight: 750;
    }
    .chip { cursor: pointer; }
    .chip.active { background: linear-gradient(135deg, #14b8a6, #34d399); color: #03131f; }
    audio { width: 100%; margin-top: 16px; accent-color: #2dd4bf; }
    .story { white-space: pre-line; }
    .link { color: #7dd3fc; word-break: break-word; }
    .empty {
        border-radius: 18px;
        border: 1px solid rgba(148,163,184,.14);
        background: rgba(2,6,23,.36);
        padding: 18px;
        color: #cbd5e1;
    }
    @media (max-width: 640px) {
        body { padding: 14px; }
        .copy, .panel { padding: 18px; }
    }
</style>
""";
    }

    private static TranslationDto? ResolvePrimaryTranslation(PoiDto poi)
    {
        return poi.Translations
            .OrderByDescending(item => string.Equals(item.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
            .ThenBy(item => item.IsFallback)
            .FirstOrDefault();
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
