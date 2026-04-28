using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Services;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Route("api/qr")]
public sealed class QrController(IQrService qrService, QrPublicLinkBuilder qrPublicLinkBuilder) : ControllerBase
{
    [Authorize(Roles = "admin")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<QrCodeDto>>>> GetAsync([FromQuery] string? targetType, CancellationToken cancellationToken)
    {
        try
        {
            var response = await qrService.GetAsync(targetType, cancellationToken);
            var hydrated = response.Select(item => qrPublicLinkBuilder.Enrich(HttpContext, item)).ToArray();
            return Ok(new ApiResponse<IReadOnlyList<QrCodeDto>> { Succeeded = true, Message = "QR codes loaded.", Data = hydrated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<IReadOnlyList<QrCodeDto>>
            {
                Succeeded = false,
                Message = "QR filter is invalid.",
                Error = new ErrorResponse { Code = "invalid_target_type", Message = ex.Message }
            });
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<QrCodeDto>>> CreateAsync(CreateQrRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await qrService.CreateAsync(request, cancellationToken);
            var hydrated = qrPublicLinkBuilder.Enrich(HttpContext, response);
            return Ok(new ApiResponse<QrCodeDto> { Succeeded = true, Message = "QR code created.", Data = hydrated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<QrCodeDto>
            {
                Succeeded = false,
                Message = "QR code creation failed.",
                Error = new ErrorResponse { Code = "invalid_target_type", Message = ex.Message }
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<QrCodeDto>
            {
                Succeeded = false,
                Message = "QR code target was not found.",
                Error = new ErrorResponse { Code = "target_not_found", Message = ex.Message }
            });
        }
    }

    [AllowAnonymous]
    [HttpGet("{code}")]
    public async Task<ActionResult<ApiResponse<QrCodeDto>>> ResolveAsync(string code, CancellationToken cancellationToken)
    {
        var response = await qrService.ResolveAsync(code, cancellationToken);
        var hydrated = qrPublicLinkBuilder.Enrich(HttpContext, response);
        return Ok(new ApiResponse<QrCodeDto> { Succeeded = true, Message = "QR code resolved.", Data = hydrated });
    }

    [AllowAnonymous]
    [HttpPost("{code}/scan")]
    public async Task<ActionResult<ApiResponse<QrCodeDto>>> ScanAsync(string code, [FromHeader(Name = "X-Device-Id")] string? deviceId, CancellationToken cancellationToken)
    {
        var response = await qrService.ScanAsync(code, string.IsNullOrWhiteSpace(deviceId) ? "anonymous-device" : deviceId, cancellationToken);
        var hydrated = qrPublicLinkBuilder.Enrich(HttpContext, response);
        return Ok(new ApiResponse<QrCodeDto> { Succeeded = true, Message = "QR code scanned.", Data = hydrated });
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await qrService.DeleteAsync(id, cancellationToken);
        return Ok(new ApiResponse<object> { Succeeded = true, Message = "QR code deleted." });
    }
}
