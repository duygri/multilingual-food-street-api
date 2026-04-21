using System.Text.Json;

namespace NarrationApp.Mobile.Features.Home;

public enum TouristApiDeploymentEnvironment
{
    Development,
    Staging,
    Production
}

public sealed class TouristApiOptions
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public TouristApiEnvironmentUrls Development { get; init; } = new();

    public TouristApiEnvironmentUrls Staging { get; init; } = new();

    public TouristApiEnvironmentUrls Production { get; init; } = new();

    public static TouristApiOptions Parse(string json)
    {
        return JsonSerializer.Deserialize<TouristApiOptions>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Tourist API configuration payload is empty.");
    }
}

public sealed class TouristApiEnvironmentUrls
{
    public string Default { get; init; } = string.Empty;

    public string? Android { get; init; }
}

public static class TouristApiEndpointResolver
{
    public static Uri Resolve(TouristApiOptions options, TouristApiDeploymentEnvironment environment, bool isAndroid)
    {
        var urls = environment switch
        {
            TouristApiDeploymentEnvironment.Development => options.Development,
            TouristApiDeploymentEnvironment.Staging => options.Staging,
            TouristApiDeploymentEnvironment.Production => options.Production,
            _ => throw new InvalidOperationException($"Unsupported API environment: {environment}.")
        };

        var rawValue = isAndroid && !string.IsNullOrWhiteSpace(urls.Android)
            ? urls.Android!
            : urls.Default;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new InvalidOperationException($"API base URL is missing for {environment}.");
        }

        var normalizedValue = rawValue.EndsWith("/", StringComparison.Ordinal) ? rawValue : $"{rawValue}/";
        if (!Uri.TryCreate(normalizedValue, UriKind.Absolute, out var baseAddress))
        {
            throw new InvalidOperationException($"API base URL for {environment} is invalid: {rawValue}");
        }

        return baseAddress;
    }
}
