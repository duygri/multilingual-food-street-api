using System.Net;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Poi;

namespace NarrationApp.Server.Services;

internal static partial class QrLaunchHtmlBuilder
{
    public static string BuildPoiLaunchHtml(
        string code,
        PoiDto poi,
        IReadOnlyList<AudioDto> audioItems,
        string appDeepLink,
        string publicUrl)
    {
        var primaryTranslation = ResolvePrimaryTranslation(poi);
        var readyAudio = SelectReadyAudio(audioItems);
        var defaultAudio = readyAudio.FirstOrDefault();

        var safeCode = WebUtility.HtmlEncode(code);
        var safeTitle = WebUtility.HtmlEncode(primaryTranslation?.Title ?? poi.Name);
        var safeCategory = WebUtility.HtmlEncode(poi.CategoryName ?? "POI");
        var safeDescription = WebUtility.HtmlEncode(primaryTranslation?.Description ?? poi.Description);
        var safeHighlight = WebUtility.HtmlEncode(primaryTranslation?.Highlight ?? string.Empty);
        var safeStory = WebUtility.HtmlEncode(primaryTranslation?.Story ?? poi.TtsScript);
        var safeImageUrl = WebUtility.HtmlEncode(poi.ImageUrl ?? string.Empty);
        var safeMapLink = WebUtility.HtmlEncode(poi.MapLink ?? string.Empty);
        var safeAppDeepLink = WebUtility.HtmlEncode(appDeepLink);
        var safePublicUrl = WebUtility.HtmlEncode(publicUrl);
        var safeDefaultAudioUrl = WebUtility.HtmlEncode(defaultAudio?.Url ?? string.Empty);
        var safeDefaultAudioLabel = WebUtility.HtmlEncode(defaultAudio is null ? "Chưa có audio sẵn sàng" : BuildAudioLabel(defaultAudio));
        var safeScanEndpoint = WebUtility.HtmlEncode($"/api/qr/{Uri.EscapeDataString(code)}/scan");
        var safePresenceEndpoint = WebUtility.HtmlEncode($"/api/qr/{Uri.EscapeDataString(code)}/presence");

        var imageBlock = string.IsNullOrWhiteSpace(poi.ImageUrl)
            ? string.Empty
            : $"""
                <div class="hero-media">
                    <img src="{safeImageUrl}" alt="{safeTitle}">
                </div>
                """;

        var mapAction = string.IsNullOrWhiteSpace(poi.MapLink)
            ? string.Empty
            : $"""<a class="button button--ghost" href="{safeMapLink}" target="_blank" rel="noopener noreferrer">Xem bản đồ</a>""";

        var highlightBlock = string.IsNullOrWhiteSpace(primaryTranslation?.Highlight)
            ? string.Empty
            : $"""<p><strong>{safeHighlight}</strong></p>""";

        var audioSection = defaultAudio is null
            ? """
                <div class="empty-audio">
                    <strong>Chưa có file nghe thử.</strong>
                    <p>POI này chưa có audio public sẵn sàng để phát trên web.</p>
                </div>
                """
            : $"""
                <div class="audio-toolbar">
                    {BuildAudioButtonsHtml(readyAudio)}
                </div>
                <div class="audio-player-shell">
                    <div class="audio-player-meta">
                        <strong>Nghe thuyết minh</strong>
                        <span id="audio-label">{safeDefaultAudioLabel}</span>
                    </div>
                    <audio id="poi-audio-player" controls preload="metadata" src="{safeDefaultAudioUrl}"></audio>
                </div>
                """;

        return $$"""
<!doctype html>
<html lang="vi">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
    <title>{{safeTitle}} · Food Street</title>
    <style>
        :root { color-scheme: dark; }
        * { box-sizing: border-box; }
        body {
            margin: 0;
            min-height: 100vh;
            padding: 24px;
            background:
                radial-gradient(circle at top, rgba(20, 184, 166, 0.14), transparent 30%),
                linear-gradient(180deg, #020617 0%, #0f172a 100%);
            color: #e2e8f0;
            font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
        }
        .shell {
            width: min(100%, 860px);
            margin: 0 auto;
            display: grid;
            gap: 18px;
        }
        .hero,
        .panel {
            border-radius: 24px;
            border: 1px solid rgba(59, 130, 246, 0.16);
            background: rgba(15, 23, 42, 0.94);
            box-shadow: 0 24px 80px rgba(2, 6, 23, 0.32);
        }
        .hero {
            overflow: hidden;
            position: relative;
        }
        .hero-media img {
            display: block;
            width: 100%;
            max-height: 340px;
            object-fit: cover;
        }
        .hero-copy,
        .panel {
            padding: 24px;
        }
        .eyebrow {
            margin: 0 0 10px;
            color: #2dd4bf;
            text-transform: uppercase;
            letter-spacing: 0.18em;
            font-size: 12px;
            font-weight: 700;
        }
        h1, h2, p { margin: 0; }
        h1 {
            font-size: clamp(2rem, 4vw, 3rem);
            line-height: 1.06;
            margin-bottom: 12px;
        }
        h2 {
            font-size: 1.2rem;
            margin-bottom: 12px;
        }
        p {
            color: #94a3b8;
            line-height: 1.65;
        }
        .hero-meta,
        .hero-actions,
        .audio-toolbar {
            display: flex;
            flex-wrap: wrap;
            gap: 12px;
        }
        .hero-meta {
            margin: 18px 0;
        }
        .pill {
            display: inline-flex;
            align-items: center;
            min-height: 36px;
            padding: 0 14px;
            border-radius: 999px;
            border: 1px solid rgba(45, 212, 191, 0.24);
            background: rgba(15, 118, 110, 0.14);
            color: #99f6e4;
            font-size: 0.92rem;
            font-weight: 600;
        }
        .hero-actions {
            margin-top: 20px;
        }
        .button {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            min-height: 48px;
            padding: 0 18px;
            border-radius: 14px;
            text-decoration: none;
            font-weight: 700;
            border: 1px solid transparent;
        }
        .button--primary {
            background: linear-gradient(135deg, #14b8a6 0%, #34d399 100%);
            color: #02131f;
        }
        .button--ghost {
            border-color: rgba(148, 163, 184, 0.22);
            color: #e2e8f0;
            background: rgba(15, 23, 42, 0.82);
        }
        .audio-toolbar {
            margin-bottom: 16px;
        }
        .hero-open-app {
            position: absolute;
            top: 18px;
            right: 18px;
            min-height: 40px;
            padding: 0 14px;
            border-radius: 999px;
            border: 1px solid rgba(148, 163, 184, 0.22);
            color: #e2e8f0;
            background: rgba(15, 23, 42, 0.86);
            text-decoration: none;
            font-weight: 700;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            box-shadow: 0 12px 28px rgba(2, 6, 23, 0.26);
        }
        .audio-chip {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            min-height: 38px;
            padding: 0 14px;
            border-radius: 999px;
            border: 1px solid rgba(45, 212, 191, 0.28);
            background: rgba(8, 47, 73, 0.54);
            color: #a5f3fc;
            font-weight: 700;
            text-transform: lowercase;
            cursor: pointer;
        }
        .audio-chip.is-active {
            background: linear-gradient(135deg, rgba(20, 184, 166, 0.92), rgba(34, 197, 94, 0.92));
            color: #04212b;
        }
        .audio-player-shell {
            display: grid;
            gap: 12px;
            border-radius: 18px;
            border: 1px solid rgba(59, 130, 246, 0.16);
            background: rgba(2, 6, 23, 0.36);
            padding: 18px;
        }
        .audio-player-meta {
            display: grid;
            gap: 4px;
        }
        .audio-player-meta span {
            color: #93c5fd;
        }
        audio {
            width: 100%;
            accent-color: #2dd4bf;
        }
        .story {
            white-space: pre-line;
        }
        .empty-audio {
            display: grid;
            gap: 8px;
            border-radius: 18px;
            border: 1px solid rgba(148, 163, 184, 0.14);
            background: rgba(15, 23, 42, 0.72);
            padding: 18px;
        }
        .meta {
            color: #7dd3fc;
            word-break: break-word;
        }
        @media (max-width: 640px) {
            body { padding: 14px; }
            .hero-copy,
            .panel { padding: 18px; }
            .hero-open-app {
                top: 12px;
                right: 12px;
                min-height: 38px;
                padding: 0 12px;
                font-size: 0.88rem;
            }
        }
    </style>
</head>
<body>
    <main class="shell">
        <section class="hero">
            <a class="hero-open-app" href="{{safeAppDeepLink}}">Mở ứng dụng</a>
            {{imageBlock}}
            <div class="hero-copy">
                <p class="eyebrow">QR POI Public</p>
                <h1>{{safeTitle}}</h1>
                <p>{{safeDescription}}</p>
                <div class="hero-meta">
                    <span class="pill">{{safeCategory}}</span>
                    <span class="pill">Mã QR {{safeCode}}</span>
                </div>
                {{highlightBlock}}
                <div class="hero-actions">
                    {{mapAction}}
                </div>
            </div>
        </section>

        <section class="panel">
            <p class="eyebrow">Audio đa ngôn ngữ</p>
            <h2>Nghe thuyết minh</h2>
            <p>Chạm vào từng ngôn ngữ để đổi audio trực tiếp trên web.</p>
            {{audioSection}}
        </section>

        <section class="panel">
            <p class="eyebrow">Nội dung POI</p>
            <h2>Câu chuyện tại điểm dừng</h2>
            <p class="story">{{safeStory}}</p>
        </section>

        <section class="panel">
            <p class="eyebrow">Liên kết QR</p>
            <p class="meta">{{safePublicUrl}}</p>
        </section>
    </main>
    <script>
        const player = document.getElementById('poi-audio-player');
        const label = document.getElementById('audio-label');
        const chips = Array.from(document.querySelectorAll('[data-audio-src]'));

        chips.forEach(function (chip) {
            chip.addEventListener('click', function () {
                if (!player) {
                    return;
                }

                chips.forEach(function (item) { item.classList.remove('is-active'); });
                chip.classList.add('is-active');
                player.src = chip.getAttribute('data-audio-src') || '';
                if (label) {
                    label.textContent = chip.getAttribute('data-audio-label') || '';
                }

                player.play().catch(function () { });
            });
        });
{{BuildPublicQrTrackingScript(safeScanEndpoint, safePresenceEndpoint, safeAppDeepLink, trackVisibilityChange: true)}}
    </script>
</body>
</html>
""";
    }

    public static string BuildPoiListLaunchHtml(
        string code,
        IReadOnlyList<PoiDto> pois,
        IReadOnlyDictionary<int, IReadOnlyList<AudioDto>> audioItemsByPoiId,
        string appDeepLink,
        string publicUrl,
        QrDeviceConfigDecision deviceConfig)
    {
        var appLaunchDeepLink = deviceConfig.BuildAppDeepLink(appDeepLink);
        var safeCode = WebUtility.HtmlEncode(code);
        var safeAppDeepLink = WebUtility.HtmlEncode(appLaunchDeepLink);
        var safePublicUrl = WebUtility.HtmlEncode(publicUrl);
        var safeDeviceLabel = deviceConfig.Label;
        var safeDeviceNotice = deviceConfig.Notice;
        var safeScanEndpoint = WebUtility.HtmlEncode($"/api/qr/{Uri.EscapeDataString(code)}/scan");
        var safePresenceEndpoint = WebUtility.HtmlEncode($"/api/qr/{Uri.EscapeDataString(code)}/presence");
        var poiCards = BuildPoiListCardsHtml(pois, audioItemsByPoiId);
        var appActionHtml = deviceConfig.ShouldOpenApp
            ? $"""<a class="button button--primary" href="{safeAppDeepLink}">Mở danh sách trong app</a>"""
            : """<span class="button button--disabled" aria-disabled="true">Dùng web fallback</span>""";

        return $$"""
<!doctype html>
<html lang="vi">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
    <title>Danh sách POI · Food Street</title>
    <style>
        :root { color-scheme: dark; }
        * { box-sizing: border-box; }
        body {
            margin: 0;
            min-height: 100vh;
            padding: 18px;
            background:
                radial-gradient(circle at 10% 0%, rgba(20, 184, 166, 0.18), transparent 28%),
                linear-gradient(180deg, #020617 0%, #0f172a 100%);
            color: #e2e8f0;
            font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
        }
        .shell {
            width: min(100%, 980px);
            margin: 0 auto;
            display: grid;
            gap: 18px;
        }
        .hero,
        .poi-card {
            border-radius: 24px;
            border: 1px solid rgba(59, 130, 246, 0.18);
            background: rgba(15, 23, 42, 0.94);
            box-shadow: 0 22px 60px rgba(2, 6, 23, 0.34);
        }
        .hero {
            padding: 24px;
            display: grid;
            gap: 16px;
        }
        .eyebrow {
            margin: 0;
            color: #2dd4bf;
            text-transform: uppercase;
            letter-spacing: 0.18em;
            font-size: 12px;
            font-weight: 800;
        }
        h1, h2, h3, p { margin: 0; }
        h1 {
            font-size: clamp(2rem, 5vw, 3.6rem);
            line-height: 1.02;
        }
        p {
            color: #94a3b8;
            line-height: 1.6;
        }
        .hero-actions,
        .poi-list,
        .poi-meta,
        .audio-toolbar {
            display: flex;
            flex-wrap: wrap;
            gap: 12px;
        }
        .hero-actions {
            align-items: center;
        }
        .button {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            min-height: 46px;
            padding: 0 18px;
            border-radius: 14px;
            text-decoration: none;
            font-weight: 800;
        }
        .button--primary {
            background: linear-gradient(135deg, #14b8a6 0%, #34d399 100%);
            color: #02131f;
        }
        .button--ghost {
            border: 1px solid rgba(148, 163, 184, 0.22);
            color: #e2e8f0;
            background: rgba(15, 23, 42, 0.82);
        }
        .button--disabled {
            border: 1px solid rgba(148, 163, 184, 0.18);
            color: #94a3b8;
            background: rgba(15, 23, 42, 0.62);
        }
        .device-notice {
            display: grid;
            gap: 6px;
            padding: 12px 14px;
            border-radius: 16px;
            border: 1px solid rgba(45, 212, 191, 0.24);
            background: rgba(15, 118, 110, 0.12);
        }
        .device-notice strong {
            color: #ccfbf1;
        }
        .poi-list {
            align-items: stretch;
        }
        .poi-card {
            flex: 1 1 300px;
            min-width: min(100%, 300px);
            overflow: hidden;
            display: grid;
        }
        .poi-card__image {
            width: 100%;
            height: 160px;
            object-fit: cover;
            background: linear-gradient(135deg, rgba(20, 184, 166, 0.18), rgba(59, 130, 246, 0.18));
        }
        .poi-card__body {
            padding: 18px;
            display: grid;
            gap: 12px;
        }
        .poi-meta {
            gap: 8px;
        }
        .pill {
            display: inline-flex;
            align-items: center;
            min-height: 30px;
            padding: 0 10px;
            border-radius: 999px;
            border: 1px solid rgba(45, 212, 191, 0.24);
            background: rgba(15, 118, 110, 0.14);
            color: #99f6e4;
            font-size: 0.85rem;
            font-weight: 700;
        }
        .audio-box {
            display: grid;
            gap: 10px;
            border-radius: 16px;
            border: 1px solid rgba(59, 130, 246, 0.16);
            background: rgba(2, 6, 23, 0.34);
            padding: 12px;
        }
        .audio-chip {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            min-height: 34px;
            padding: 0 12px;
            border-radius: 999px;
            border: 1px solid rgba(45, 212, 191, 0.28);
            background: rgba(8, 47, 73, 0.54);
            color: #a5f3fc;
            font-weight: 800;
            text-transform: lowercase;
            cursor: pointer;
        }
        .audio-chip.is-active {
            background: linear-gradient(135deg, rgba(20, 184, 166, 0.92), rgba(34, 197, 94, 0.92));
            color: #04212b;
        }
        audio {
            width: 100%;
            accent-color: #2dd4bf;
        }
        .empty-audio {
            color: #94a3b8;
            font-weight: 700;
        }
        .meta {
            color: #7dd3fc;
            word-break: break-word;
            font-size: 0.9rem;
        }
        @media (max-width: 640px) {
            body { padding: 12px; }
            .hero,
            .poi-card__body { padding: 16px; }
            .poi-card__image { height: 136px; }
        }
    </style>
</head>
<body>
    <main class="shell">
        <section class="hero">
            <p class="eyebrow">QR danh sách POI</p>
            <h1>Danh sách điểm tham quan</h1>
            <p>Quét một mã để xem các POI ẩm thực Vĩnh Khánh. Nếu chưa cài app, bạn vẫn có thể mở từng điểm và nghe audio theo ngôn ngữ sẵn có ngay trên web.</p>
            <p class="meta">Quy ước server: 0 = cấu hình mạnh, 1 = cấu hình yếu.</p>
            <div class="hero-actions">
                {{appActionHtml}}
                <a class="button button--ghost" href="{{safePublicUrl}}">Tải lại trang QR</a>
            </div>
            <div class="device-notice" data-qr-device-config="{{deviceConfig.Value}}">
                <strong>{{safeDeviceLabel}}</strong>
                <span>{{safeDeviceNotice}}</span>
            </div>
            <p class="meta">Mã QR {{safeCode}} · {{safePublicUrl}}</p>
        </section>

        <section class="poi-list" aria-label="Danh sách POI">
            {{poiCards}}
        </section>
    </main>
    <script>
        document.querySelectorAll('[data-audio-src]').forEach(function (chip) {
            chip.addEventListener('click', function () {
                const card = chip.closest('[data-poi-card]');
                if (!card) {
                    return;
                }

                const player = card.querySelector('audio');
                const label = card.querySelector('[data-audio-label]');
                if (!player) {
                    return;
                }

                card.querySelectorAll('[data-audio-src]').forEach(function (item) {
                    item.classList.remove('is-active');
                });
                chip.classList.add('is-active');
                player.src = chip.getAttribute('data-audio-src') || '';
                if (label) {
                    label.textContent = chip.getAttribute('data-audio-label') || '';
                }
                player.play().catch(function () { });
            });
        });
{{BuildPublicQrTrackingScript(safeScanEndpoint, safePresenceEndpoint, safeAppDeepLink, trackVisibilityChange: false, deviceConfig)}}
    </script>
</body>
</html>
""";
    }

}
