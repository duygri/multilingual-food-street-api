using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;
using NarrationApp.Server.Services;

namespace NarrationApp.Server.Tests.Services.Integrations;

public sealed class LiveExternalSmokeTests
{
    [Fact]
    public async Task Google_translation_v3_returns_translated_text()
    {
        if (!ShouldRun())
        {
            return;
        }

        var (googleOptions, _) = LoadOptions();
        Assert.True(googleOptions.IsConfigured, "Google Cloud credentials are not configured.");

        var tokenProvider = new GoogleServiceAccountTokenProvider(Options.Create(googleOptions));
        using var httpClient = new HttpClient();
        var projectId = ResolveProjectId(googleOptions);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://translate.googleapis.com/v3/projects/{Uri.EscapeDataString(projectId)}/locations/global:translateText")
        {
            Content = JsonContent.Create(new
            {
                contents = new[] { "Bún bò Huế là một món ăn nổi tiếng của Việt Nam." },
                mimeType = "text/plain",
                sourceLanguageCode = "vi",
                targetLanguageCode = "en"
            })
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokenProvider.GetAccessTokenAsync());

        using var response = await httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.IsSuccessStatusCode,
            $"Translation v3 failed with {(int)response.StatusCode} {response.ReasonPhrase}: {body}");

        Assert.Contains("translatedText", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Google_tts_api_returns_audio_content()
    {
        if (!ShouldRun())
        {
            return;
        }

        var (googleOptions, _) = LoadOptions();
        Assert.True(googleOptions.IsConfigured, "Google Cloud credentials are not configured.");

        var tokenProvider = new GoogleServiceAccountTokenProvider(Options.Create(googleOptions));
        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://texttospeech.googleapis.com/v1/text:synthesize")
        {
            Content = JsonContent.Create(new
            {
                input = new
                {
                    text = "Hue beef noodle soup is a famous Vietnamese dish."
                },
                voice = new
                {
                    languageCode = "en-US",
                    name = "en-US-Standard-A"
                },
                audioConfig = new
                {
                    audioEncoding = "MP3"
                }
            })
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokenProvider.GetAccessTokenAsync());

        using var response = await httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.IsSuccessStatusCode,
            $"Text-to-Speech failed with {(int)response.StatusCode} {response.ReasonPhrase}: {body}");

        Assert.Contains("audioContent", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cloudflare_r2_upload_read_and_delete_work()
    {
        if (!ShouldRun())
        {
            return;
        }

        var (_, r2Options) = LoadOptions();
        Assert.True(r2Options.IsConfigured, "Cloudflare R2 credentials are not configured.");

        var credentials = new BasicAWSCredentials(r2Options.AccessKeyId, r2Options.SecretAccessKey);
        var s3Config = new AmazonS3Config
        {
            ServiceURL = r2Options.ServiceUrl,
            AuthenticationRegion = "auto",
            ForcePathStyle = true
        };

        using var s3Client = new AmazonS3Client(credentials, s3Config);
        var objectClient = new AwsR2ObjectClient(s3Client);
        var storageService = new CloudflareR2StorageService(objectClient, Options.Create(r2Options));

        var audioBytes = new byte[] { 1, 2, 3, 4, 5, 6 };
        Assert.NotEmpty(audioBytes);

        await using var content = new MemoryStream(audioBytes);
        var saved = await storageService.SaveAsync("live-smoke-en.mp3", content);

        try
        {
            Assert.StartsWith("audio/", saved.StoragePath, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(saved.Url));

            await using var uploaded = await storageService.OpenReadAsync(saved.StoragePath);
            Assert.True(uploaded.Length > 0);
        }
        finally
        {
            await storageService.DeleteAsync(saved.StoragePath);
        }
    }

    [Fact]
    public async Task Google_translation_tts_and_r2_pipeline_work_end_to_end()
    {
        if (!ShouldRun())
        {
            return;
        }

        var (googleOptions, r2Options) = LoadOptions();
        Assert.True(googleOptions.IsConfigured, "Google Cloud credentials are not configured.");
        Assert.True(r2Options.IsConfigured, "Cloudflare R2 credentials are not configured.");

        var tokenProvider = new GoogleServiceAccountTokenProvider(Options.Create(googleOptions));
        using var translationHttpClient = new HttpClient();
        using var ttsHttpClient = new HttpClient();

        var translationService = new GoogleCloudTranslationService(
            translationHttpClient,
            tokenProvider,
            Options.Create(googleOptions));
        var ttsService = new GoogleCloudTtsService(ttsHttpClient, tokenProvider);

        var credentials = new BasicAWSCredentials(r2Options.AccessKeyId, r2Options.SecretAccessKey);
        var s3Config = new AmazonS3Config
        {
            ServiceURL = r2Options.ServiceUrl,
            AuthenticationRegion = "auto",
            ForcePathStyle = true
        };

        using var s3Client = new AmazonS3Client(credentials, s3Config);
        var objectClient = new AwsR2ObjectClient(s3Client);
        var storageService = new CloudflareR2StorageService(objectClient, Options.Create(r2Options));

        const string sourceText = "Bún bò Huế là một món ăn nổi tiếng của Việt Nam.";
        var translatedText = await translationService.TranslateAsync(sourceText, "vi", "en");

        Assert.False(string.IsNullOrWhiteSpace(translatedText));
        Assert.NotEqual(sourceText, translatedText);

        var audioBytes = await ttsService.GenerateAsync(translatedText, "en", "standard");
        Assert.True(audioBytes.Length > 128, $"Generated audio payload is unexpectedly small: {audioBytes.Length} bytes.");

        await using var uploadStream = new MemoryStream(audioBytes, writable: false);
        var saved = await storageService.SaveAsync("live-smoke-e2e-en.mp3", uploadStream);

        try
        {
            Assert.StartsWith("audio/", saved.StoragePath, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(saved.Url));

            await using var uploaded = await storageService.OpenReadAsync(saved.StoragePath);
            using var downloaded = new MemoryStream();
            await uploaded.CopyToAsync(downloaded);

            Assert.Equal(audioBytes.Length, downloaded.Length);
        }
        finally
        {
            await storageService.DeleteAsync(saved.StoragePath);
        }
    }

    private static bool ShouldRun()
    {
        return string.Equals(Environment.GetEnvironmentVariable("NARRATIONAPP_RUN_LIVE_SMOKE"), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static (GoogleCloudOptions Google, CloudflareR2Options R2) LoadOptions()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(@"D:\VinhKhanhFoodStreet\src\NarrationApp.Server")
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        return (
            configuration.GetSection(GoogleCloudOptions.SectionName).Get<GoogleCloudOptions>() ?? new GoogleCloudOptions(),
            configuration.GetSection(CloudflareR2Options.SectionName).Get<CloudflareR2Options>() ?? new CloudflareR2Options());
    }

    private static string ResolveProjectId(GoogleCloudOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ProjectId))
        {
            return options.ProjectId.Trim();
        }

        using var stream = File.OpenRead(options.CredentialsFilePath);
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.GetProperty("project_id").GetString()
            ?? throw new InvalidOperationException("Google Cloud project_id is missing from credentials.");
    }
}
