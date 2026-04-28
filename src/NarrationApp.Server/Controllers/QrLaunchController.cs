using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Services;

namespace NarrationApp.Server.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
[Route("qr")]
public sealed class QrLaunchController(IQrService qrService, QrPublicLinkBuilder qrPublicLinkBuilder) : ControllerBase
{
    [HttpGet("{code}")]
    public async Task<IActionResult> LaunchAsync(string code, CancellationToken cancellationToken)
    {
        try
        {
            var qrCode = await qrService.ResolveAsync(code, cancellationToken);
            var appDeepLink = qrPublicLinkBuilder.BuildAppDeepLink(qrCode.Code);
            var publicUrl = qrPublicLinkBuilder.BuildPublicUrl(HttpContext, qrCode.Code);

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
        <p class="eyebrow">QR Launch</p>
        <h1>Mở Food Street</h1>
        <p>Điện thoại của bạn vừa quét một mã QR hợp lệ. Chạm nút bên dưới để mở ứng dụng Food Street.</p>
        <div class="code">{{safeCode}}</div>
        <div class="actions">
            <a class="button button--primary" href="{{safeDeepLink}}">Mở ứng dụng</a>
            <a class="button button--ghost" href="{{safePublicUrl}}">Tải lại trang QR</a>
        </div>
        <div class="meta">
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
