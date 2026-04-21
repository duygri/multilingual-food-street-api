using System.Text.Json;
using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;

namespace NarrationApp.Server.Services;

public sealed class GoogleCloudTranslationService(
    HttpClient httpClient,
    IGoogleAccessTokenProvider accessTokenProvider,
    IOptions<GoogleCloudOptions> options) : IGoogleTranslationService
{
    public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var projectId = ResolveProjectId(options.Value);
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://translate.googleapis.com/v3/projects/{Uri.EscapeDataString(projectId)}/locations/global:translateText")
        {
            Content = JsonContent.Create(new
            {
                contents = new[] { text },
                mimeType = "text/plain",
                sourceLanguageCode = NormalizeLanguageCode(sourceLanguage),
                targetLanguageCode = NormalizeLanguageCode(targetLanguage)
            })
        };

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await accessTokenProvider.GetAccessTokenAsync(cancellationToken));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "Google Cloud Translation", cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<TranslationResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Google Cloud Translation returned an empty response.");

        return payload.Translations.FirstOrDefault()?.TranslatedText
            ?? throw new InvalidOperationException("Google Cloud Translation did not return translated text.");
    }

    private static string ResolveProjectId(GoogleCloudOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ProjectId))
        {
            return options.ProjectId.Trim();
        }

        if (string.IsNullOrWhiteSpace(options.CredentialsFilePath))
        {
            throw new InvalidOperationException("Google Cloud project ID could not be resolved because credentials are not configured.");
        }

        using var stream = File.OpenRead(options.CredentialsFilePath);
        using var document = JsonDocument.Parse(stream);
        if (document.RootElement.TryGetProperty("project_id", out var projectIdElement))
        {
            var projectId = projectIdElement.GetString();
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                return projectId;
            }
        }

        throw new InvalidOperationException("Google Cloud project_id was not found in the credentials file.");
    }

    private static string NormalizeLanguageCode(string languageCode)
    {
        return languageCode.Trim();
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string serviceName, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"{serviceName} request failed with {(int)response.StatusCode} {response.ReasonPhrase}: {body}",
            null,
            response.StatusCode);
    }

    private sealed class TranslationResponse
    {
        public IReadOnlyList<TranslationItem> Translations { get; init; } = Array.Empty<TranslationItem>();
    }

    private sealed class TranslationItem
    {
        public string TranslatedText { get; init; } = string.Empty;
    }
}
