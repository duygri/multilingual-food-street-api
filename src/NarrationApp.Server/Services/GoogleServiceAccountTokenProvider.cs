using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;

namespace NarrationApp.Server.Services;

public sealed class GoogleServiceAccountTokenProvider : IGoogleAccessTokenProvider
{
    private static readonly string[] Scopes =
    [
        "https://www.googleapis.com/auth/cloud-platform"
    ];

    private readonly GoogleCredential _credential;

    public GoogleServiceAccountTokenProvider(IOptions<GoogleCloudOptions> options)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.CredentialsFilePath))
        {
            throw new InvalidOperationException("Google Cloud credentials file path is not configured.");
        }

        _credential = CredentialFactory
            .FromFile<ServiceAccountCredential>(settings.CredentialsFilePath)
            .ToGoogleCredential()
            .CreateScoped(Scopes);
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);
    }
}
