namespace NarrationApp.Server.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "NarrationApp.Server";

    public string Audience { get; init; } = "NarrationApp.Client";

    public string SigningKey { get; init; } = "replace-this-development-signing-key-with-a-long-random-secret";

    public int ExpiresInMinutes { get; init; } = 480;
}
