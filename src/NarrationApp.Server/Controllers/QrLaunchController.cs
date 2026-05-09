using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Services;

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
    private const string HtmlContentType = "text/html; charset=utf-8";

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
                return await BuildPoiLaunchResponseAsync(qrCode.Code, qrCode.TargetId, appDeepLink, publicUrl, cancellationToken);
            }

            return Html(QrLaunchHtmlBuilder.BuildLaunchHtml(qrCode.Code, qrCode.TargetType, appDeepLink, publicUrl));
        }
        catch (KeyNotFoundException)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return Html(QrLaunchHtmlBuilder.BuildErrorHtml("Không tìm thấy mã QR này."));
        }
        catch (InvalidOperationException)
        {
            Response.StatusCode = StatusCodes.Status410Gone;
            return Html(QrLaunchHtmlBuilder.BuildErrorHtml("Mã QR này đã hết hạn."));
        }
    }

    private async Task<IActionResult> BuildPoiLaunchResponseAsync(
        string code,
        int poiId,
        string appDeepLink,
        string publicUrl,
        CancellationToken cancellationToken)
    {
        var poi = await poiService.GetByIdAsync(poiId, cancellationToken);
        if (poi is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return Html(QrLaunchHtmlBuilder.BuildErrorHtml("Không tìm thấy POI cho mã QR này."));
        }

        var audioItems = await audioService.GetByPoiAsync(poiId, languageCode: null, cancellationToken);
        return Html(QrLaunchHtmlBuilder.BuildPoiLaunchHtml(code, poi, audioItems, appDeepLink, publicUrl));
    }

    private ContentResult Html(string html)
    {
        return Content(html, HtmlContentType, Encoding.UTF8);
    }
}
