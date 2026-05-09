using System.Net;
using System.Text;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorWalkingDirectionsServiceTests
{
    [Fact]
    public async Task LoadWalkingRouteAsync_RequestsMapboxWalkingProfileAndMapsGeoJsonRoute()
    {
        HttpRequestMessage? capturedRequest = null;
        var httpClient = new HttpClient(new FakeHttpMessageHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(CreateJsonResponse(
                """
                {
                  "code": "Ok",
                  "routes": [
                    {
                      "distance": 184.4,
                      "duration": 172.0,
                      "geometry": {
                        "type": "LineString",
                        "coordinates": [
                          [106.705400, 10.760900],
                          [106.706100, 10.761400],
                          [106.707700, 10.762200]
                        ]
                      }
                    }
                  ]
                }
                """));
        }));
        var service = new VisitorWalkingDirectionsService(
            httpClient,
            new VisitorMapOptions { AccessToken = "pk.test-token" });
        var origin = new VisitorLocationSnapshot(true, true, 10.760900, 106.705400, "Đã định vị");
        var destination = CreatePoi(latitude: 10.762200, longitude: 106.707700);

        var result = await service.LoadWalkingRouteAsync(origin, destination);

        Assert.True(result.IsAvailable);
        Assert.NotNull(result.Route);
        Assert.Equal("Đi bộ 184 m • khoảng 3 phút", result.StatusLabel);
        Assert.Equal(184, result.Route!.DistanceMeters);
        Assert.Equal(3, result.Route.DurationMinutes);
        Assert.Equal(
            [10.760900, 10.761400, 10.762200],
            result.Route.Points.Select(point => Math.Round(point.Latitude, 6)));
        Assert.Equal(
            [106.705400, 106.706100, 106.707700],
            result.Route.Points.Select(point => Math.Round(point.Longitude, 6)));

        Assert.NotNull(capturedRequest);
        var requestUrl = capturedRequest!.RequestUri!.ToString();
        Assert.Contains("/directions/v5/mapbox/walking/106.705400,10.760900;106.707700,10.762200", requestUrl, StringComparison.Ordinal);
        Assert.Contains("geometries=geojson", requestUrl, StringComparison.Ordinal);
        Assert.Contains("overview=full", requestUrl, StringComparison.Ordinal);
        Assert.Contains("access_token=pk.test-token", requestUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadWalkingRouteAsync_ReturnsUnavailableWithoutCurrentLocationAndDoesNotCallMapbox()
    {
        var callCount = 0;
        var service = new VisitorWalkingDirectionsService(
            new HttpClient(new FakeHttpMessageHandler((_, _) =>
            {
                callCount++;
                return Task.FromResult(CreateJsonResponse("""{"code":"Ok","routes":[]}"""));
            })),
            new VisitorMapOptions { AccessToken = "pk.test-token" });

        var result = await service.LoadWalkingRouteAsync(VisitorLocationSnapshot.Disabled(), CreatePoi());

        Assert.False(result.IsAvailable);
        Assert.Null(result.Route);
        Assert.Equal("Chưa có vị trí hiện tại để vẽ đường đi bộ.", result.StatusLabel);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public async Task LoadWalkingRouteAsync_ReturnsUnavailableWhenMapboxTokenIsMissing()
    {
        var callCount = 0;
        var service = new VisitorWalkingDirectionsService(
            new HttpClient(new FakeHttpMessageHandler((_, _) =>
            {
                callCount++;
                return Task.FromResult(CreateJsonResponse("""{"code":"Ok","routes":[]}"""));
            })),
            new VisitorMapOptions { AccessToken = "YOUR_MAPBOX_ACCESS_TOKEN_HERE" });

        var origin = new VisitorLocationSnapshot(true, true, 10.760900, 106.705400, "Đã định vị");
        var result = await service.LoadWalkingRouteAsync(origin, CreatePoi());

        Assert.False(result.IsAvailable);
        Assert.Null(result.Route);
        Assert.Equal("Chưa có Mapbox token để vẽ đường đi trong app.", result.StatusLabel);
        Assert.Equal(0, callCount);
    }

    private static VisitorPoi CreatePoi(double latitude = 10.762200, double longitude = 106.707700) =>
        new(
            "poi-1",
            "Ốc Oanh",
            "hai-san",
            "Hải sản",
            "Quận 4",
            "Live API",
            "desc",
            "highlight",
            18,
            52,
            180,
            "3:12",
            "Sẵn sàng",
            latitude,
            longitude);

    private static HttpResponseMessage CreateJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
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
