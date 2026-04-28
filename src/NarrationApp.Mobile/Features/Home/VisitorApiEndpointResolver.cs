using System.Text.Json;

namespace NarrationApp.Mobile.Features.Home;

public enum VisitorApiDeploymentEnvironment
{
    Development,
    Staging,
    Production
}

public sealed class VisitorApiOptions
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public VisitorApiEnvironmentUrls Development { get; init; } = new();

    public VisitorApiEnvironmentUrls Staging { get; init; } = new();

    public VisitorApiEnvironmentUrls Production { get; init; } = new();

    public static VisitorApiOptions Parse(string json)
    {
        return JsonSerializer.Deserialize<VisitorApiOptions>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Visitor API configuration payload is empty.");
    }
}

public sealed class VisitorApiEnvironmentUrls
{
    public string Default { get; init; } = string.Empty;

    public string? Android { get; init; }
}

public static class VisitorApiEndpointResolver
{
    public static Uri Resolve(VisitorApiOptions options, VisitorApiDeploymentEnvironment environment, bool isAndroid)
    {
        var urls = environment switch
        {
            VisitorApiDeploymentEnvironment.Development => options.Development,
            VisitorApiDeploymentEnvironment.Staging => options.Staging,
            VisitorApiDeploymentEnvironment.Production => options.Production,
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
