namespace NarrationApp.Server.Configuration;

public sealed class GoogleCloudOptions
{
    public const string SectionName = "GoogleCloud";

    public string CredentialsFilePath { get; init; } = string.Empty;

    public string ProjectId { get; init; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(CredentialsFilePath);
}
