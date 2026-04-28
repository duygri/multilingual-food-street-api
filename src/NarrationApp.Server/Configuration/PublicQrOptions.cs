namespace NarrationApp.Server.Configuration;

public sealed class PublicQrOptions
{
    public const string SectionName = "PublicQr";

    public string BaseUrl { get; init; } = string.Empty;

    public int LoopbackPublicPort { get; init; } = 5000;

    public string AppScheme { get; init; } = "foodstreet";
}
