namespace NarrationApp.Server.Configuration;

public sealed class CloudflareR2Options
{
    public const string SectionName = "CloudflareR2";

    public string AccountId { get; init; } = string.Empty;

    public string AccessKeyId { get; init; } = string.Empty;

    public string SecretAccessKey { get; init; } = string.Empty;

    public string BucketName { get; init; } = string.Empty;

    public string PublicBaseUrl { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountId)
        && !string.IsNullOrWhiteSpace(AccessKeyId)
        && !string.IsNullOrWhiteSpace(SecretAccessKey)
        && !string.IsNullOrWhiteSpace(BucketName);

    public string ServiceUrl => $"https://{AccountId}.r2.cloudflarestorage.com";
}
