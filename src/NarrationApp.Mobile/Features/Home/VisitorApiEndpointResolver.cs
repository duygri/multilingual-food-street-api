using System.Text.Json;

namespace NarrationApp.Mobile.Features.Home;

public enum VisitorApiDeploymentEnvironment
{
    Development,
    Staging,
    Production
}

public enum VisitorApiClientPlatform
{
    Default,
    AndroidEmulator,
    AndroidDevice
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

    public string? AndroidEmulator { get; init; }

    public string? AndroidDevice { get; init; }
}

public static class VisitorApiEndpointResolver
{
    public static Uri Resolve(VisitorApiOptions options, VisitorApiDeploymentEnvironment environment, VisitorApiClientPlatform platform)
    {
        var urls = environment switch
        {
            VisitorApiDeploymentEnvironment.Development => options.Development,
            VisitorApiDeploymentEnvironment.Staging => options.Staging,
            VisitorApiDeploymentEnvironment.Production => options.Production,
            _ => throw new InvalidOperationException($"Unsupported API environment: {environment}.")
        };

        var rawValue = ResolveRawValue(urls, platform);
        if (ShouldUseDevelopmentFallback(rawValue, environment, platform))
        {
            var developmentFallback = ResolveRawValue(options.Development, platform);
            if (TryCreateAbsoluteUri(developmentFallback, out var developmentBaseAddress))
            {
                return developmentBaseAddress;
            }
        }

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new InvalidOperationException($"API base URL is missing for {environment}.");
        }

        if (!TryCreateAbsoluteUri(rawValue, out var baseAddress))
        {
            throw new InvalidOperationException($"API base URL for {environment} is invalid: {rawValue}");
        }

        return baseAddress;
    }

    private static string ResolveRawValue(VisitorApiEnvironmentUrls urls, VisitorApiClientPlatform platform)
    {
        return platform switch
        {
            VisitorApiClientPlatform.AndroidEmulator when !string.IsNullOrWhiteSpace(urls.AndroidEmulator) => urls.AndroidEmulator!,
            VisitorApiClientPlatform.AndroidDevice when !string.IsNullOrWhiteSpace(urls.AndroidDevice) => urls.AndroidDevice!,
            VisitorApiClientPlatform.AndroidEmulator or VisitorApiClientPlatform.AndroidDevice when !string.IsNullOrWhiteSpace(urls.Android) => urls.Android!,
            _ => urls.Default
        };
    }

    private static bool ShouldUseDevelopmentFallback(
        string rawValue,
        VisitorApiDeploymentEnvironment environment,
        VisitorApiClientPlatform platform)
    {
        return environment is not VisitorApiDeploymentEnvironment.Development
            && platform is VisitorApiClientPlatform.AndroidDevice or VisitorApiClientPlatform.AndroidEmulator
            && TryCreateAbsoluteUri(rawValue, out var baseAddress)
            && (string.Equals(baseAddress.Host, "api.foodstreet.example", StringComparison.OrdinalIgnoreCase)
                || string.Equals(baseAddress.Host, "staging-api.foodstreet.example", StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryCreateAbsoluteUri(string rawValue, out Uri baseAddress)
    {
        baseAddress = new Uri("http://localhost/", UriKind.Absolute);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var normalizedValue = rawValue.EndsWith("/", StringComparison.Ordinal) ? rawValue : $"{rawValue}/";
        if (!Uri.TryCreate(normalizedValue, UriKind.Absolute, out var parsedBaseAddress))
        {
            return false;
        }

        baseAddress = parsedBaseAddress;
        return true;
    }
}
