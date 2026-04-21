using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.SharedUI.Auth;

namespace NarrationApp.Web.Services;

public sealed class ApiClient(HttpClient httpClient, IAuthSessionStore sessionStore, CustomAuthStateProvider? authStateProvider = null)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> SessionPreservingUnauthorizedErrorCodes =
    [
        "invalid_credentials",
        "invalid_current_password"
    ];

    public Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken = default)
    {
        return SendForDataAsync<T>(new HttpRequestMessage(HttpMethod.Get, uri), cancellationToken);
    }

    public Task<TResponse> PostAsync<TRequest, TResponse>(string uri, TRequest request, CancellationToken cancellationToken = default)
    {
        return SendWithBodyAsync<TRequest, TResponse>(HttpMethod.Post, uri, request, cancellationToken);
    }

    public Task<TResponse> PutAsync<TRequest, TResponse>(string uri, TRequest request, CancellationToken cancellationToken = default)
    {
        return SendWithBodyAsync<TRequest, TResponse>(HttpMethod.Put, uri, request, cancellationToken);
    }

    public async Task PutAsync<TRequest>(string uri, TRequest request, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(request, SerializerOptions), Encoding.UTF8, "application/json")
        };

        await SendWithoutDataAsync(message, cancellationToken);
    }

    public Task<TResponse> PostMultipartAsync<TResponse>(string uri, MultipartFormDataContent content, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = content
        };

        return SendForDataAsync<TResponse>(request, cancellationToken);
    }

    public async Task PutAsync(string uri, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, uri);
        await SendWithoutDataAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(string uri, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, uri);
        await SendWithoutDataAsync(request, cancellationToken);
    }

    private async Task<TResponse> SendWithBodyAsync<TRequest, TResponse>(HttpMethod method, string uri, TRequest requestBody, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody, SerializerOptions), Encoding.UTF8, "application/json")
        };

        return await SendForDataAsync<TResponse>(request, cancellationToken);
    }

    private async Task<TResponse> SendForDataAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await SendAsync<TResponse>(request, cancellationToken);
        if (response.Data is null)
        {
            throw new ApiException(response.Error?.Message ?? response.Message, System.Net.HttpStatusCode.OK, response.Error?.Code);
        }

        return response.Data;
    }

    private async Task SendWithoutDataAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _ = await SendAsync<object?>(request, cancellationToken);
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

        await InvalidateExpiredSessionAsync(response.StatusCode, envelope.Error?.Code, cancellationToken);

        throw new ApiException(envelope.Error?.Message ?? envelope.Message, response.StatusCode, envelope.Error?.Code);
    }

    private async Task AttachAuthorizationAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = await sessionStore.GetAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(session?.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        }
    }

    private async Task InvalidateExpiredSessionAsync(HttpStatusCode statusCode, string? errorCode, CancellationToken cancellationToken)
    {
        if (statusCode != HttpStatusCode.Unauthorized
            || authStateProvider is null
            || (errorCode is not null && SessionPreservingUnauthorizedErrorCodes.Contains(errorCode)))
        {
            return;
        }

        if (await sessionStore.GetAsync(cancellationToken) is null)
        {
            return;
        }

        await authStateProvider.MarkUserAsLoggedOutAsync();
    }

    private static async Task<ApiResponse<TResponse>> ReadEnvelopeAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is 0)
        {
            return new ApiResponse<TResponse> { Succeeded = response.IsSuccessStatusCode, Message = response.ReasonPhrase ?? string.Empty };
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(SerializerOptions, cancellationToken);
        return envelope ?? new ApiResponse<TResponse>
        {
            Succeeded = false,
            Message = response.ReasonPhrase ?? "Unexpected empty response."
        };
    }
}
