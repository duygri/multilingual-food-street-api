using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Tour;

namespace NarrationApp.Mobile.Features.Home;

public interface ITouristTourSessionApiService
{
    Task<TourSessionDto?> GetLatestAsync(CancellationToken cancellationToken = default);

    Task<TourSessionDto> StartAsync(int tourId, string? deviceId = null, CancellationToken cancellationToken = default);

    Task<TourSessionDto> ResumeAsync(int tourId, CancellationToken cancellationToken = default);

    Task<TourSessionDto> ProgressAsync(int tourId, UpdateTourProgressRequest request, CancellationToken cancellationToken = default);
}

public sealed class TouristTourSessionApiService(HttpClient httpClient, ITouristAuthSessionStore sessionStore) : ITouristTourSessionApiService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<TourSessionDto?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await SendAsync<TourSessionDto?>(new HttpRequestMessage(HttpMethod.Get, "api/tours/session/latest"), cancellationToken);
            return response.Data;
        }
        catch (TouristApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            await sessionStore.ClearAsync(cancellationToken);
            return null;
        }
    }

    public Task<TourSessionDto> StartAsync(int tourId, string? deviceId = null, CancellationToken cancellationToken = default)
    {
        return SendWithBodyAsync<object?, TourSessionDto>(
            HttpMethod.Post,
            $"api/tours/{tourId}/start",
            string.IsNullOrWhiteSpace(deviceId) ? null : new { deviceId },
            cancellationToken);
    }

    public Task<TourSessionDto> ResumeAsync(int tourId, CancellationToken cancellationToken = default)
    {
        return SendWithBodyAsync<object?, TourSessionDto>(HttpMethod.Post, $"api/tours/{tourId}/resume", null, cancellationToken);
    }

    public Task<TourSessionDto> ProgressAsync(int tourId, UpdateTourProgressRequest request, CancellationToken cancellationToken = default)
    {
        return SendWithBodyAsync<UpdateTourProgressRequest, TourSessionDto>(HttpMethod.Post, $"api/tours/{tourId}/progress", request, cancellationToken);
    }

    private async Task<TResponse> SendWithBodyAsync<TRequest, TResponse>(
        HttpMethod method,
        string uri,
        TRequest requestBody,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (requestBody is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody, SerializerOptions), Encoding.UTF8, "application/json");
        }

        var response = await SendAsync<TResponse>(request, cancellationToken);
        if (response.Data is null)
        {
            throw new TouristApiException(response.Error?.Message ?? response.Message, HttpStatusCode.OK, response.Error?.Code);
        }

        return response.Data;
    }

    private async Task<ApiResponse<TResponse>> SendAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var envelope = await ReadEnvelopeAsync<TResponse>(response, cancellationToken);
        if (response.IsSuccessStatusCode && envelope.Succeeded)
        {
            return envelope;
        }

        throw new TouristApiException(envelope.Error?.Message ?? envelope.Message, response.StatusCode, envelope.Error?.Code);
    }

    private async Task AttachAuthorizationAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = await sessionStore.GetAsync(cancellationToken);
        if (session is null || session.IsExpired() || string.IsNullOrWhiteSpace(session.Token))
        {
            await sessionStore.ClearAsync(cancellationToken);
            throw new TouristApiException("Tourist session is not available.", HttpStatusCode.Unauthorized, "tourist_session_missing");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
    }

    private static async Task<ApiResponse<TResponse>> ReadEnvelopeAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is 0)
        {
            return new ApiResponse<TResponse>
            {
                Succeeded = response.IsSuccessStatusCode,
                Message = response.ReasonPhrase ?? string.Empty
            };
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(SerializerOptions, cancellationToken);
        return envelope ?? new ApiResponse<TResponse>
        {
            Succeeded = false,
            Message = response.ReasonPhrase ?? "Unexpected empty response."
        };
    }
}
