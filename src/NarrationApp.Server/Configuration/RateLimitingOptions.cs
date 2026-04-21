namespace NarrationApp.Server.Configuration;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicyOptions Auth { get; init; } = new()
    {
        PermitLimit = 6,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    public RateLimitPolicyOptions Mutation { get; init; } = new()
    {
        PermitLimit = 30,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    public RateLimitPolicyOptions Generation { get; init; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60,
        QueueLimit = 0
    };
}

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; init; }

    public int WindowSeconds { get; init; }

    public int QueueLimit { get; init; }
}
