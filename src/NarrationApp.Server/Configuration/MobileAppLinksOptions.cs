namespace NarrationApp.Server.Configuration;

public sealed class MobileAppLinksOptions
{
    public const string SectionName = "MobileAppLinks";

    public IReadOnlyList<AndroidAppLinkTargetOptions> Android { get; init; } = [];
}

public sealed class AndroidAppLinkTargetOptions
{
    public string PackageName { get; init; } = string.Empty;

    public IReadOnlyList<string> Sha256CertFingerprints { get; init; } = [];
}
