using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Auth;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Services;

public sealed class AdminPortalServiceTests
{
    [Fact]
    public async Task GetMovementFlowsAsync_sends_filter_query()
    {
        var handler = new InspectingAnalyticsHandler();
        var apiClient = new ApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5001/")
        }, new TestAuthSessionStore());
        var sut = new AdminPortalService(apiClient);

        var result = await sut.GetMovementFlowsAsync(new MovementFlowQueryDto
        {
            TimeRange = HeatmapTimeRange.Last24Hours,
            EventTypeFilter = EventType.AudioPlay,
            MinimumUniqueSessions = 5
        });

        Assert.Single(result);
        Assert.Equal("api/analytics/movement-flows?timeRange=Last24Hours&eventTypeFilter=AudioPlay&minimumUniqueSessions=5", handler.RequestUris[0]);
    }

    private sealed class TestAuthSessionStore : IAuthSessionStore
    {
        public ValueTask<AuthSession?> GetAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<AuthSession?>(null);
        }

        public ValueTask SetAsync(AuthSession session, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class InspectingAnalyticsHandler : HttpMessageHandler
    {
        public List<string> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri?.PathAndQuery.TrimStart('/') ?? string.Empty);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ApiResponse<IReadOnlyList<MovementFlowDto>>
                {
                    Succeeded = true,
                    Message = "ok",
                    Data =
                    [
                        new MovementFlowDto
                        {
                            FromPoiId = 1,
                            FromPoiName = "A",
                            FromLat = 10.75,
                            FromLng = 106.7,
                            ToPoiId = 2,
                            ToPoiName = "B",
                            ToLat = 10.76,
                            ToLng = 106.71,
                            Weight = 5,
                            UniqueSessions = 5
                        }
                    ]
                }, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            });
        }
    }
}
