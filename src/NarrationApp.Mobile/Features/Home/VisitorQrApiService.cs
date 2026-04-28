using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Mobile.Features.Home;

public interface IVisitorQrApiService
{
    Task<QrCodeDto> OpenAsync(string code, string deviceId, CancellationToken cancellationToken = default);
}

public sealed class VisitorQrApiService(HttpClient httpClient) : IVisitorQrApiService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<QrCodeDto> OpenAsync(string code, string deviceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var normalizedCode = code.Trim();
        var normalizedDeviceId = string.IsNullOrWhiteSpace(deviceId) ? "anonymous-device" : deviceId.Trim();
        VisitorMobileDiagnostics.Log(
            "QrApi",
            $"OpenAsync code={normalizedCode} device={normalizedDeviceId} base={httpClient.BaseAddress}");

        try
        {
            return await ScanAsync(normalizedCode, normalizedDeviceId, cancellationToken);
        }
        catch (VisitorApiException)
        {
            VisitorMobileDiagnostics.Log("QrApi", $"Scan failed for code={normalizedCode}; falling back to resolve");
            return await ResolveAsync(normalizedCode, cancellationToken);
        }
    }

    private async Task<QrCodeDto> ScanAsync(string code, string deviceId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/qr/{Uri.EscapeDataString(code)}/scan");
        request.Headers.Add("X-Device-Id", deviceId);
        VisitorMobileDiagnostics.Log("QrApi", $"POST {request.RequestUri}");
        return await SendAsync(request, cancellationToken);
    }

    private async Task<QrCodeDto> ResolveAsync(string code, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/qr/{Uri.EscapeDataString(code)}");
        VisitorMobileDiagnostics.Log("QrApi", $"GET {request.RequestUri}");
        return await SendAsync(request, cancellationToken);
    }

    private async Task<QrCodeDto> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var envelope = await ReadEnvelopeAsync(response, cancellationToken);
        VisitorMobileDiagnostics.Log(
            "QrApi",
            $"Response {(int)response.StatusCode} succeeded={response.IsSuccessStatusCode} envelopeSucceeded={envelope.Succeeded} message={envelope.Message ?? "<null>"} error={envelope.Error?.Message ?? "<null>"}");
        if (!response.IsSuccessStatusCode || !envelope.Succeeded || envelope.Data is null)
        {
            throw new VisitorApiException(
                envelope.Error?.Message ?? envelope.Message ?? "QR request failed.",
                response.StatusCode == 0 ? HttpStatusCode.InternalServerError : response.StatusCode,
                envelope.Error?.Code);
        }

        VisitorMobileDiagnostics.Log(
            "QrApi",
            $"Resolved code={envelope.Data.Code} targetType={envelope.Data.TargetType} targetId={envelope.Data.TargetId}");
        return envelope.Data;
    }

    private static async Task<ApiResponse<QrCodeDto>> ReadEnvelopeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is 0)
        {
            return new ApiResponse<QrCodeDto>
            {
                Succeeded = response.IsSuccessStatusCode,
                Message = response.ReasonPhrase ?? string.Empty
            };
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<QrCodeDto>>(SerializerOptions, cancellationToken);
        return envelope ?? new ApiResponse<QrCodeDto>
        {
            Succeeded = false,
            Message = response.ReasonPhrase ?? "Unexpected empty response."
        };
    }
}
