namespace NarrationApp.Web.Configuration;

public sealed class QrPublicUrlOptions
{
    public Uri BaseAddress { get; init; } = new Uri("https://narration.app/", UriKind.Absolute);
}
