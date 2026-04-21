using System.Text.Json;
using Microsoft.Maui.Storage;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Services;

public sealed class SecureTouristAuthSessionStore : ITouristAuthSessionStore
{
    private const string StorageKey = "foodstreet.mobile.tourist-auth-session";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<TouristAuthSession?> GetAsync(CancellationToken cancellationToken = default)
    {
        var payload = await ReadPayloadAsync();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        return JsonSerializer.Deserialize<TouristAuthSession>(payload, SerializerOptions);
    }

    public async ValueTask SetAsync(TouristAuthSession session, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(session, SerializerOptions);

        try
        {
            await SecureStorage.Default.SetAsync(StorageKey, payload);
        }
        catch
        {
            Preferences.Default.Set(StorageKey, payload);
        }
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            SecureStorage.Default.Remove(StorageKey);
        }
        catch
        {
            // Best-effort clear for devices where secure storage isn't available yet.
        }

        Preferences.Default.Remove(StorageKey);
        return ValueTask.CompletedTask;
    }

    private static async Task<string?> ReadPayloadAsync()
    {
        try
        {
            var securePayload = await SecureStorage.Default.GetAsync(StorageKey);
            if (!string.IsNullOrWhiteSpace(securePayload))
            {
                return securePayload;
            }
        }
        catch
        {
            // Fall back to preferences when secure storage isn't available.
        }

        return Preferences.Default.Get<string?>(StorageKey, null);
    }
}
