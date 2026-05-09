using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Services;

namespace NarrationApp.Server.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
[Route("share")]
public sealed class ShareController(
    IPoiService poiService,
    IAudioService audioService,
    ITourService tourService) : ControllerBase
{
    private const string HtmlContentType = "text/html; charset=utf-8";

    [HttpGet("poi/{id:int}")]
    public async Task<IActionResult> PoiAsync(int id, CancellationToken cancellationToken)
    {
        var poi = await poiService.GetByIdAsync(id, cancellationToken);
        if (poi is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return Html(QrLaunchHtmlBuilder.BuildErrorHtml("Không tìm thấy POI để chia sẻ."));
        }

        var audioItems = await audioService.GetByPoiAsync(id, languageCode: null, cancellationToken);
        return Html(SharePreviewHtmlBuilder.BuildPoiShareHtml(poi, audioItems, BuildPublicUrl()));
    }

    [HttpGet("tour/{id:int}")]
    public async Task<IActionResult> TourAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourService.GetByIdAsync(id, includeUnpublished: false, cancellationToken);
            return Html(SharePreviewHtmlBuilder.BuildTourShareHtml(tour, BuildPublicUrl()));
        }
        catch (KeyNotFoundException)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return Html(QrLaunchHtmlBuilder.BuildErrorHtml("Không tìm thấy tour để chia sẻ."));
        }
    }

    private string BuildPublicUrl()
    {
        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{Request.QueryString}";
    }

    private ContentResult Html(string html)
    {
        return Content(html, HtmlContentType, Encoding.UTF8);
    }
}
