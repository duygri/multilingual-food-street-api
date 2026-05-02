using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Services;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
[Route("qr")]
public sealed class QrLaunchController(
    IQrService qrService,
    QrPublicLinkBuilder qrPublicLinkBuilder,
    IPoiService poiService,
    IAudioService audioService) : ControllerBase
{
    [HttpGet("{code}")]
    public async Task<IActionResult> LaunchAsync(string code, CancellationToken cancellationToken)
    {
        try
        {
            var qrCode = await qrService.ResolveAsync(code, cancellationToken);
            var appDeepLink = qrPublicLinkBuilder.BuildAppDeepLink(qrCode.Code);
            var publicUrl = qrPublicLinkBuilder.BuildPublicUrl(HttpContext, qrCode.Code);

            if (string.Equals(qrCode.TargetType, "poi", StringComparison.OrdinalIgnoreCase))
            {
                var poi = await poiService.GetByIdAsync(qrCode.TargetId, cancellationToken);
                if (poi is null)
                {
                    Response.StatusCode = StatusCodes.Status404NotFound;
                    return Content(BuildErrorHtml("Không tìm thấy POI cho mã QR này."), "text/html; charset=utf-8", Encoding.UTF8);
                }

                var audioItems = await audioService.GetByPoiAsync(qrCode.TargetId, languageCode: null, cancellationToken);
                return Content(BuildPoiLaunchHtml(qrCode.Code, poi, audioItems, appDeepLink, publicUrl), "text/html; charset=utf-8", Encoding.UTF8);
            }

            return Content(BuildLaunchHtml(qrCode.Code, appDeepLink, publicUrl), "text/html; charset=utf-8", Encoding.UTF8);
        }
        catch (KeyNotFoundException)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return Content(BuildErrorHtml("Không tìm thấy mã QR này."), "text/html; charset=utf-8", Encoding.UTF8);
        }
        catch (InvalidOperationException)
        {
            Response.StatusCode = StatusCodes.Status410Gone;
            return Content(BuildErrorHtml("Mã QR này đã hết hạn."), "text/html; charset=utf-8", Encoding.UTF8);
        }
    }

    private static string BuildPoiLaunchHtml(
        string code,
        PoiDto poi,
        IReadOnlyList<AudioDto> audioItems,
        string appDeepLink,
        string publicUrl)
    {
        var primaryTranslation = ResolvePrimaryTranslation(poi);
        var readyAudio = audioItems
            .Where(item => item.Status == AudioStatus.Ready && !string.IsNullOrWhiteSpace(item.Url))
            .OrderByDescending(item => string.Equals(item.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
            .ThenBy(item => item.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
        const scanEndpoint = "{{safeScanEndpoint}}";
        const presenceEndpoint = "{{safePresenceEndpoint}}";
        const scanStorageKey = "foodstreet.qr.public.device-id";
        const heartbeatIntervalMs = 15000;

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

        function generateVisitorDeviceId() {
            const randomToken = Math.random().toString(36).slice(2, 10);
            const timeToken = Date.now().toString(36);
            return "qr-web-" + randomToken + timeToken;
        }

        function getOrCreateVisitorDeviceId() {
            try {
                const existing = window.localStorage.getItem(scanStorageKey);
                if (existing && existing.trim().length > 0) {
                    return existing;
                }

                const generated = window.crypto && typeof window.crypto.randomUUID === 'function'
                    ? "qr-web-" + window.crypto.randomUUID()
                    : generateVisitorDeviceId();

                window.localStorage.setItem(scanStorageKey, generated);
                return generated;
            } catch (error) {
                return generateVisitorDeviceId();
            }
        }

        async function trackPublicQrVisit() {
            try {
                await window.fetch(scanEndpoint, {
                    method: 'POST',
                    keepalive: true,
                    credentials: 'same-origin',
                    headers: {
                        'X-Device-Id': getOrCreateVisitorDeviceId()
                    }
                });
            } catch (error) {
            }
        }

        async function trackPublicQrPresence() {
            try {
                await window.fetch(presenceEndpoint, {
                    method: 'POST',
                    keepalive: true,
                    credentials: 'same-origin',
                    headers: {
                        'X-Device-Id': getOrCreateVisitorDeviceId()
                    }
                });
            } catch (error) {
            }
        }

        trackPublicQrVisit();
        trackPublicQrPresence();
        window.setInterval(trackPublicQrPresence, heartbeatIntervalMs);
        document.addEventListener('visibilitychange', function () {
            if (!document.hidden) {
                trackPublicQrPresence();
            }
        });
    </script>
</body>
</html>
""";
    }

    private static string BuildLaunchHtml(string code, string appDeepLink, string publicUrl)
    {
        var safeCode = WebUtility.HtmlEncode(code);
        var safeDeepLink = WebUtility.HtmlEncode(appDeepLink);
        var safePublicUrl = WebUtility.HtmlEncode(publicUrl);

        return $$"""
<!doctype html>
<html lang="vi">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
    <title>Mở Food Street</title>
    <style>
        :root { color-scheme: dark; }
        * { box-sizing: border-box; }
        body {
            margin: 0;
            min-height: 100vh;
            display: grid;
            place-items: center;
            padding: 24px;
            background: linear-gradient(180deg, #020617 0%, #0f172a 100%);
            color: #e2e8f0;
            font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
        }
        .shell {
            width: min(100%, 460px);
            border-radius: 24px;
            border: 1px solid rgba(59, 130, 246, 0.18);
            background: rgba(15, 23, 42, 0.94);
            box-shadow: 0 24px 80px rgba(2, 6, 23, 0.45);
            padding: 28px;
        }
        .eyebrow {
            margin: 0 0 10px;
            color: #38bdf8;
            text-transform: uppercase;
            letter-spacing: 0.18em;
            font-size: 12px;
            font-weight: 700;
        }
        h1 {
            margin: 0 0 12px;
            font-size: 32px;
            line-height: 1.08;
        }
        p {
            margin: 0 0 14px;
            color: #94a3b8;
            line-height: 1.6;
        }
        .code {
            display: inline-flex;
            margin: 8px 0 18px;
            padding: 10px 14px;
            border-radius: 999px;
            background: rgba(15, 118, 110, 0.18);
            color: #5eead4;
            font-weight: 700;
            letter-spacing: 0.06em;
        }
        .actions {
            display: flex;
            gap: 12px;
            flex-wrap: wrap;
            margin: 8px 0 18px;
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
        .meta {
            margin-top: 18px;
            padding-top: 18px;
            border-top: 1px solid rgba(148, 163, 184, 0.14);
        }
        .meta a {
            color: #38bdf8;
            word-break: break-word;
        }
    </style>
</head>
<body>
    <main class="shell">
        <p class="eyebrow">QR mở app</p>
        <h1>Launcher mở Food Street</h1>
        <p>Đây là mã QR dành riêng cho luồng mở ứng dụng. Chạm nút bên dưới để chuyển sang app Food Street.</p>
        <div class="code">{{safeCode}}</div>
        <div class="actions">
            <a class="button button--primary" href="{{safeDeepLink}}">Mở ứng dụng</a>
            <a class="button button--ghost" href="{{safePublicUrl}}">Tải lại trang QR</a>
        </div>
        <div class="meta">
            <p>QR này khác với QR POI. Nó không hiển thị nội dung thuyết minh, mà chỉ đóng vai trò launcher vào app.</p>
            <p>Nếu ứng dụng chưa tự bật, hãy chạm lại nút <strong>Mở ứng dụng</strong>.</p>
            <p><a href="{{safeDeepLink}}">{{safeDeepLink}}</a></p>
        </div>
    </main>
    <script>
        window.setTimeout(function () {
            window.location.href = "{{safeDeepLink}}";
        }, 180);
    </script>
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
            var activeClass = index == 0 ? " is-active" : string.Empty;
            var safeUrl = WebUtility.HtmlEncode(audio.Url);
            var safeLabel = WebUtility.HtmlEncode(BuildAudioLabel(audio));
            var safeCode = WebUtility.HtmlEncode(audio.LanguageCode);
            builder.Append($"""<button type="button" class="audio-chip{activeClass}" data-audio-src="{safeUrl}" data-audio-label="{safeLabel}">{safeCode}</button>""");
        }

        return builder.ToString();
    }

    private static string BuildAudioLabel(AudioDto audio) => $"{audio.LanguageCode.ToUpperInvariant()} • {audio.SourceType}";

    private static TranslationDto? ResolvePrimaryTranslation(PoiDto poi)
    {
        return poi.Translations
            .OrderByDescending(item => string.Equals(item.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
            .ThenBy(item => item.IsFallback)
            .FirstOrDefault();
    }

    private static string BuildErrorHtml(string message)
    {
        var safeMessage = WebUtility.HtmlEncode(message);

        return $$"""
<!doctype html>
<html lang="vi">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
    <title>QR không hợp lệ</title>
    <style>
        body {
            margin: 0;
            min-height: 100vh;
            display: grid;
            place-items: center;
            padding: 24px;
            background: #020617;
            color: #e2e8f0;
            font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
        }
        .shell {
            width: min(100%, 420px);
            padding: 28px;
            border-radius: 24px;
            border: 1px solid rgba(239, 68, 68, 0.18);
            background: rgba(15, 23, 42, 0.94);
        }
        h1 { margin: 0 0 12px; font-size: 30px; }
        p { margin: 0; color: #94a3b8; line-height: 1.6; }
    </style>
</head>
<body>
    <main class="shell">
        <h1>QR không dùng được</h1>
        <p>{{safeMessage}}</p>
    </main>
</body>
</html>
""";
    }
}
