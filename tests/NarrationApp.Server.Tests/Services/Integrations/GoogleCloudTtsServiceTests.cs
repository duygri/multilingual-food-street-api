using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;
using NarrationApp.Server.Services;

namespace NarrationApp.Server.Tests.Services.Integrations;

public sealed class GoogleCloudTtsServiceTests
{
    [Fact]
    public async Task GenerateAsync_posts_synthesize_request_and_decodes_audio_content()
    {
        var expectedBytes = new byte[] { 9, 8, 7, 6 };
        var handler = new RecordingHandler($$"""
            {
              "audioContent": "{{Convert.ToBase64String(expectedBytes)}}"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var sut = new GoogleCloudTtsService(httpClient, new StubGoogleAccessTokenProvider("test-token"));

        var result = await sut.GenerateAsync("Xin chao", "vi-VN", "wavenet");

        Assert.Equal(expectedBytes, result);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://texttospeech.googleapis.com/v1/text:synthesize", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "test-token"), handler.LastRequest.Headers.Authorization);
        var body = await handler.LastRequest.Content!.ReadAsStringAsync();
        Assert.Contains("\"text\":\"Xin chao\"", body);
        Assert.Contains("\"languageCode\":\"vi-VN\"", body);
        Assert.Contains("\"name\":\"vi-VN-Wavenet-A\"", body);
        Assert.Contains("\"audioEncoding\":\"MP3\"", body);
    }

    [Fact]
    public async Task GenerateAsync_falls_back_when_requested_voice_tier_is_unavailable_for_language()
    {
        var handler = new RecordingHandler("""
            {
              "audioContent": "AQID"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var sut = new GoogleCloudTtsService(httpClient, new StubGoogleAccessTokenProvider("test-token"));

        _ = await sut.GenerateAsync("Ni hao", "zh", "neural2");

        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"languageCode\":\"cmn-CN\"", body);
        Assert.Contains("\"name\":\"cmn-CN-Wavenet-A\"", body);
    }

    [Fact]
    public async Task GenerateAsync_includes_response_body_when_google_returns_error()
    {
        var handler = new RecordingHandler(
            """{"error":{"message":"Cloud Text-to-Speech API disabled"}}""",
            HttpStatusCode.Forbidden);
        using var httpClient = new HttpClient(handler);
        var sut = new GoogleCloudTtsService(httpClient, new StubGoogleAccessTokenProvider("test-token"));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.GenerateAsync("Xin chao", "vi-VN", "standard"));

        Assert.Contains("Cloud Text-to-Speech API disabled", exception.Message);
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
