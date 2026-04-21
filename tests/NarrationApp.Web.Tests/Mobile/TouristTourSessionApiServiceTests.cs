using System.Net;
using System.Net.Http.Json;
using NarrationApp.Mobile.Features.Home;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class TouristTourSessionApiServiceTests
{
    [Fact]
    public async Task StartAsync_AttachesBearerTokenAndReturnsSession()
    {
        var expiresAtUtc = DateTime.UtcNow.AddHours(6);
        var sessionStore = new InMemoryTouristAuthSessionStore(
            new TouristAuthSession(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "tourist@example.com",
                "vi",
                UserRole.Tourist,
                "tourist-token",
                expiresAtUtc));

        var service = CreateService(sessionStore, (request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/tours/9/start", request.RequestUri!.AbsolutePath);
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("tourist-token", request.Headers.Authorization!.Parameter);

            return Task.FromResult(CreateJsonResponse(new ApiResponse<TourSessionDto>
            {
                Succeeded = true,
                Data = new TourSessionDto
                {
                    Id = 14,
                    TourId = 9,
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Status = TourSessionStatus.InProgress,
                    CurrentStopSequence = 0,
                    TotalStops = 4,
                    StartedAtUtc = new DateTime(2026, 4, 19, 8, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2026, 4, 19, 8, 0, 0, DateTimeKind.Utc)
                }
            }));
        });

        var session = await service.StartAsync(9);

        Assert.Equal(9, session.TourId);
        Assert.Equal(TourSessionStatus.InProgress, session.Status);
        Assert.Equal(4, session.TotalStops);
    }

    private static TouristTourSessionApiService CreateService(
        ITouristAuthSessionStore sessionStore,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://10.0.2.2:5001/")
        };

        return new TouristTourSessionApiService(httpClient, sessionStore);
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

    private sealed class InMemoryTouristAuthSessionStore(TouristAuthSession? session = null) : ITouristAuthSessionStore
    {
        private TouristAuthSession? _session = session;

        public ValueTask<TouristAuthSession?> GetAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(_session);

        public ValueTask SetAsync(TouristAuthSession session, CancellationToken cancellationToken = default)
        {
            _session = session;
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            _session = null;
            return ValueTask.CompletedTask;
        }
    }
}
