using System.Text.Json;
using Microsoft.JSInterop;
using NarrationApp.SharedUI.Auth;

namespace NarrationApp.Web.Services;

public sealed class BrowserAuthSessionStore(IJSRuntime jsRuntime) : IAuthSessionStore
{
    private const string StorageKey = "narration-app.auth-session";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<AuthSession?> GetAsync(CancellationToken cancellationToken = default)
    {
        var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, StorageKey);
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<AuthSession>(json, SerializerOptions);
    }

    public ValueTask SetAsync(AuthSession session, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(session, SerializerOptions);
        return jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, StorageKey, json);
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        return jsRuntime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, StorageKey);
    }
}
