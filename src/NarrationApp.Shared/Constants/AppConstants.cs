namespace NarrationApp.Shared.Constants;

public static class AppConstants
{
    public const string DefaultLanguage = "vi";
    public const string DefaultAdminEmail = "admin@narration.app";
    public const string DefaultAdminPassword = "Admin@123";
    public const string DefaultOwnerEmail = "owner@narration.app";
    public const string DefaultOwnerPassword = "Owner@123";
    public const int DefaultGeofenceRadiusMeters = 30;
    public const int DefaultDebounceSeconds = 10;
    public const int DefaultCooldownSeconds = 1800;
    public const int DefaultTourStopRadiusMeters = 30;
    public const string DefaultCorsPolicyName = "NarrationAppCors";
    public const string AuthRateLimitPolicyName = "auth";
    public const string ContentMutationRateLimitPolicyName = "content-mutation";
    public const string GenerationRateLimitPolicyName = "generation";
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string HttpContextUserIdKey = "UserId";
    public const string HttpContextUserRoleKey = "UserRole";

    public static IReadOnlyList<string> SupportedLanguages { get; } = new[]
    {
        "vi",
        "en",
        "ja",
        "ko",
        "zh"
    };

    public static IReadOnlyList<string> DefaultAllowedOrigins { get; } = new[]
    {
        "http://localhost:5000",
        "https://localhost:5001",
        "http://localhost:5100",
        "http://127.0.0.1:5100",
        "http://localhost:5173",
        "https://localhost:5173",
        "http://localhost:5285",
        "https://localhost:7055"
    };
}
