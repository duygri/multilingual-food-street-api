using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NarrationApp.Shared.DTOs.Auth;
using NarrationApp.Shared.DTOs.Common;

namespace NarrationApp.Mobile.Features.Home;

public interface ITouristAuthApiService
{
    Task<TouristAuthSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<TouristAuthSession> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<TouristAuthSession?> GetCurrentSessionAsync(CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);
}

public sealed class TouristAuthApiService(HttpClient httpClient, ITouristAuthSessionStore sessionStore) : ITouristAuthApiService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<TouristAuthSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendWithBodyAsync<LoginRequest, AuthResponse>(HttpMethod.Post, "api/auth/login-tourist", request, authorize: false, cancellationToken);
        var session = Map(response);
        await sessionStore.SetAsync(session, cancellationToken);
        return session;
    }

    public async Task<TouristAuthSession> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendWithBodyAsync<RegisterRequest, AuthResponse>(HttpMethod.Post, "api/auth/register", request, authorize: false, cancellationToken);
        var session = Map(response);
        await sessionStore.SetAsync(session, cancellationToken);
        return session;
    }

    public async Task<TouristAuthSession?> GetCurrentSessionAsync(CancellationToken cancellationToken = default)
    {
        var existingSession = await sessionStore.GetAsync(cancellationToken);
        if (existingSession is null || existingSession.IsExpired())
        {
            await sessionStore.ClearAsync(cancellationToken);
            return null;
        }

        try
        {
            var response = await SendAsync<AuthResponse>(new HttpRequestMessage(HttpMethod.Get, "api/auth/me"), authorize: true, cancellationToken);
            if (response.Data is null)
            {
                throw new TouristApiException(response.Error?.Message ?? response.Message, HttpStatusCode.OK, response.Error?.Code);
            }

            var session = Map(response.Data);
            await sessionStore.SetAsync(session, cancellationToken);
            return session;
        }
        catch (TouristApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            await sessionStore.ClearAsync(cancellationToken);
            return null;
        }
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        return sessionStore.ClearAsync(cancellationToken).AsTask();
    }

    private async Task<TResponse> SendWithBodyAsync<TRequest, TResponse>(
        HttpMethod method,
        string uri,
        TRequest requestBody,
        bool authorize,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody, SerializerOptions), Encoding.UTF8, "application/json")
        };

        var response = await SendAsync<TResponse>(request, authorize, cancellationToken);
        if (response.Data is null)
        {
            throw new TouristApiException(response.Error?.Message ?? response.Message, HttpStatusCode.OK, response.Error?.Code);
        }

        return response.Data;
    }

    private async Task<ApiResponse<TResponse>> SendAsync<TResponse>(HttpRequestMessage request, bool authorize, CancellationToken cancellationToken)
    {
        if (authorize)
        {
            await AttachAuthorizationAsync(request, cancellationToken);
        }

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

    private static TouristAuthSession Map(AuthResponse response)
    {
        return new TouristAuthSession(
            response.UserId,
            response.Email,
            response.PreferredLanguage,
            response.Role,
            response.Token,
            response.ExpiresAtUtc);
    }
}
