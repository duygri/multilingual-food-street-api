using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NarrationApp.Mobile.Features.Home;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorAudioPlayReporterTests
{
    [Fact]
    public async Task TrackAsync_posts_audio_play_visit_event_for_server_poi()
    {
        HttpRequestMessage? capturedRequest = null;
        JsonDocument? capturedPayload = null;
        var reporter = CreateReporter(
            "android-pixel-5555",
            async (request, cancellationToken) =>
            {
                capturedRequest = request;
                capturedPayload = await JsonDocument.ParseAsync(
                    await request.Content!.ReadAsStreamAsync(cancellationToken),
                    cancellationToken: cancellationToken);
                return CreateJsonResponse(new ApiResponse<object> { Succeeded = true });
            });

        await reporter.TrackAsync(
            new VisitorAudioCue(
                PoiId: "poi-17",
                LanguageCode: "en",
                StreamUrl: "https://cdn.example/audio/17-en.mp3",
                DurationSeconds: 123,
                IsAvailable: true,
                StatusLabel: "Sẵn sàng phát",
                IsPreferredLanguage: true),
            new VisitorLocationSnapshot(true, true, 10.7609, 106.7054, "Đã định vị"));

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/api/visit-events", capturedRequest.RequestUri!.AbsolutePath);
        Assert.NotNull(capturedPayload);
        var root = capturedPayload.RootElement;
        Assert.Equal("android-pixel-5555", root.GetProperty("deviceId").GetString());
        Assert.Equal(17, root.GetProperty("poiId").GetInt32());
        Assert.Equal((int)EventType.AudioPlay, root.GetProperty("eventType").GetInt32());
        Assert.Equal("mobile-audio", root.GetProperty("source").GetString());
        Assert.Equal(123, root.GetProperty("listenDurationSeconds").GetInt32());
        Assert.Equal(10.7609, root.GetProperty("lat").GetDouble(), precision: 4);
        Assert.Equal(106.7054, root.GetProperty("lng").GetDouble(), precision: 4);
    }

    [Fact]
    public async Task TrackAsync_skips_demo_only_poi_ids()
    {
        var reporter = CreateReporter(
            "android-pixel-5555",
            (_, _) => throw new InvalidOperationException("Demo POIs should not call visit-events."));

        await reporter.TrackAsync(
            new VisitorAudioCue(
                PoiId: "poi-khanh-hoi-bridge",
                LanguageCode: "vi",
                StreamUrl: "demo.mp3",
                DurationSeconds: 91,
                IsAvailable: true,
                StatusLabel: "Demo",
                IsPreferredLanguage: true),
            VisitorLocationSnapshot.Disabled());
    }

    private static VisitorAudioPlayReporter CreateReporter(
        string deviceId,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://10.0.2.2:5001/")
        };

        return new VisitorAudioPlayReporter(httpClient, new FakeVisitorDeviceIdentityProvider(deviceId));
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(payload)
        };
    }

    private sealed class FakeVisitorDeviceIdentityProvider(string deviceId) : IVisitorDeviceIdentityProvider
    {
        public ValueTask<string> GetDeviceIdAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(deviceId);
    }

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return handler(request, cancellationToken);
        }
    }
}
