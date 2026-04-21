using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;
using NarrationApp.Server.Services;

namespace NarrationApp.Server.Tests.Services.Integrations;

public sealed class GoogleCloudTranslationServiceTests
{
    [Fact]
    public async Task TranslateAsync_posts_v3_request_with_bearer_token_and_returns_translation()
    {
        var handler = new RecordingHandler("""
            {
              "translations": [
                {
                  "translatedText": "Hello world"
                }
              ]
            }
            """);
        using var httpClient = new HttpClient(handler);
        var sut = new GoogleCloudTranslationService(
            httpClient,
            new StubGoogleAccessTokenProvider("test-token"),
            Options.Create(new GoogleCloudOptions
            {
                ProjectId = "demo-project"
            }));

        var result = await sut.TranslateAsync("Xin chao", "vi", "en");

        Assert.Equal("Hello world", result);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://translate.googleapis.com/v3/projects/demo-project/locations/global:translateText", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "test-token"), handler.LastRequest.Headers.Authorization);
        var body = await handler.LastRequest.Content!.ReadAsStringAsync();
        Assert.Contains("\"contents\":[\"Xin chao\"]", body);
        Assert.Contains("\"sourceLanguageCode\":\"vi\"", body);
        Assert.Contains("\"targetLanguageCode\":\"en\"", body);
    }

    [Fact]
    public async Task TranslateAsync_includes_response_body_when_google_returns_error()
    {
        var handler = new RecordingHandler(
            """{"error":{"message":"Cloud Translation API disabled"}}""",
            HttpStatusCode.Forbidden);
        using var httpClient = new HttpClient(handler);
        var sut = new GoogleCloudTranslationService(
            httpClient,
            new StubGoogleAccessTokenProvider("test-token"),
            Options.Create(new GoogleCloudOptions
            {
                ProjectId = "demo-project"
            }));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.TranslateAsync("Xin chao", "vi", "en"));

        Assert.Contains("Cloud Translation API disabled", exception.Message);
    }

    private sealed class StubGoogleAccessTokenProvider(string token) : IGoogleAccessTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(token);
        }
    }

    private sealed class RecordingHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        }
    }
}
