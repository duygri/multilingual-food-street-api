using System.Net;
using System.Net.Http.Json;
using NarrationApp.Mobile.Features.Home;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorAudioCatalogServiceTests
{
    [Fact]
    public async Task LoadBestForPoiAsync_PrefersSelectedLanguageReadyAsset()
    {
        var response = new ApiResponse<IReadOnlyList<AudioDto>>
        {
            Succeeded = true,
            Data =
            [
                new AudioDto
                {
                    Id = 11,
                    PoiId = 7,
                    LanguageCode = "vi",
                    SourceType = AudioSourceType.Recorded,
                    Url = "/api/audio/11/stream",
                    Status = AudioStatus.Ready,
                    DurationSeconds = 91
                },
                new AudioDto
                {
                    Id = 12,
                    PoiId = 7,
                    LanguageCode = "en",
                    SourceType = AudioSourceType.Tts,
                    Url = "/api/audio/12/stream",
                    Status = AudioStatus.Ready,
                    DurationSeconds = 88
                }
            ]
        };

        var service = CreateService((request, cancellationToken) =>
        {
            Assert.Equal("/api/audio?poiId=7", request.RequestUri!.PathAndQuery);
            return Task.FromResult(CreateJsonResponse(response));
        });

        var cue = await service.LoadBestForPoiAsync("poi-7", "en");

        Assert.True(cue.IsAvailable);
        Assert.Equal("en", cue.LanguageCode);
        Assert.Equal("https://10.0.2.2:5001/api/audio/12/stream", cue.StreamUrl);
        Assert.Equal(88, cue.DurationSeconds);
    }

    [Fact]
    public async Task LoadBestForPoiAsync_FallsBackToVietnameseWhenSelectedLanguageMissing()
    {
        var response = new ApiResponse<IReadOnlyList<AudioDto>>
        {
            Succeeded = true,
            Data =
            [
                new AudioDto
                {
                    Id = 20,
                    PoiId = 9,
                    LanguageCode = "vi",
                    SourceType = AudioSourceType.Recorded,
                    Url = "/api/audio/20/stream",
                    Status = AudioStatus.Ready,
                    DurationSeconds = 120
                },
                new AudioDto
                {
                    Id = 21,
                    PoiId = 9,
                    LanguageCode = "ja",
                    SourceType = AudioSourceType.Tts,
                    Url = "/api/audio/21/stream",
                    Status = AudioStatus.Generating,
                    DurationSeconds = 0
                }
            ]
        };

        var service = CreateService((request, cancellationToken) => Task.FromResult(CreateJsonResponse(response)));

        var cue = await service.LoadBestForPoiAsync("poi-9", "en");

        Assert.True(cue.IsAvailable);
        Assert.Equal("vi", cue.LanguageCode);
        Assert.Contains("Tiếng Việt", cue.StatusLabel);
    }

    [Fact]
    public async Task LoadBestForPoiAsync_ReturnsUnavailableWhenPoiIdIsNotServerBased()
    {
        var service = CreateService((request, cancellationToken) =>
            throw new InvalidOperationException("Should not call API for demo-only ids."));

        var cue = await service.LoadBestForPoiAsync("poi-khanh-hoi-bridge", "vi");

        Assert.False(cue.IsAvailable);
        Assert.Contains("demo", cue.StatusLabel, StringComparison.OrdinalIgnoreCase);
    }

    private static VisitorAudioCatalogService CreateService(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://10.0.2.2:5001/")
        };

        return new VisitorAudioCatalogService(httpClient);
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(payload)
        };
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
