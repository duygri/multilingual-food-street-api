using System.Net;
using System.Text;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

internal static partial class QrLaunchHtmlBuilder
{
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

    private static IReadOnlyList<AudioDto> SelectReadyAudio(IReadOnlyList<AudioDto> audioItems)
    {
        return audioItems
            .Where(item => item.Status == AudioStatus.Ready && !string.IsNullOrWhiteSpace(item.Url))
            .OrderByDescending(item => string.Equals(item.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
            .ThenBy(item => item.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string BuildPublicQrTrackingScript(
        string safeScanEndpoint,
        string safePresenceEndpoint,
        string safeAppDeepLink,
        bool trackVisibilityChange,
        QrDeviceConfigDecision? deviceConfig = null)
    {
        var visibilityTrackingScript = trackVisibilityChange
            ? """
        document.addEventListener('visibilitychange', function () {
            if (!document.hidden) {
                trackPublicQrPresence();
            }
        });
"""
            : string.Empty;

        var appOpenScript = deviceConfig is not null
            ? deviceConfig.ShouldOpenApp
                ? $$"""
        // Server service đã quyết định deviceConfig={{deviceConfig.Value}} trước khi render HTML.
        window.setTimeout(function () {
            window.location.href = "{{safeAppDeepLink}}";
        }, 180);
"""
                : $$"""
        // Server service đã quyết định deviceConfig={{deviceConfig.Value}}, giữ web fallback.
        console.info("QR deviceConfig={{deviceConfig.Value}}: cấu hình yếu, giữ web fallback.");
"""
            : $$"""
            window.setTimeout(function () {
                window.location.href = "{{safeAppDeepLink}}";
            }, 180);
""";

        return $$"""
        const scanEndpoint = "{{safeScanEndpoint}}";
        const presenceEndpoint = "{{safePresenceEndpoint}}";
        const scanStorageKey = "foodstreet.qr.public.device-id";
        const heartbeatIntervalMs = 4000;

{{appOpenScript}}

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
                    headers: { 'X-Device-Id': getOrCreateVisitorDeviceId() }
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
                    headers: { 'X-Device-Id': getOrCreateVisitorDeviceId() }
                });
            } catch (error) {
            }
        }

        trackPublicQrVisit();
        trackPublicQrPresence();
        window.setInterval(trackPublicQrPresence, heartbeatIntervalMs);
{{visibilityTrackingScript}}
""";
    }

    private static TranslationDto? ResolvePrimaryTranslation(PoiDto poi)
    {
        return poi.Translations
            .OrderByDescending(item => string.Equals(item.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
            .ThenBy(item => item.IsFallback)
            .FirstOrDefault();
    }
}
