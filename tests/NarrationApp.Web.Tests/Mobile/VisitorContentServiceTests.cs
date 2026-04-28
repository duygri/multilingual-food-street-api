using System.Net;
using System.Net.Http.Json;
using NarrationApp.Mobile.Features.Home;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Geofence;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorContentServiceTests
{
    [Fact]
    public async Task LoadAsync_ReturnsLiveContentWhenApiSucceeds()
    {
        var poiResponse = new ApiResponse<IReadOnlyList<PoiDto>>
        {
            Succeeded = true,
            Data =
            [
                new PoiDto
                {
                    Id = 7,
                    Name = "Bến Nhà Rồng",
                    Slug = "ben-nha-rong",
                    Lat = 10.7609,
                    Lng = 106.7054,
                    CategoryName = "Di tích",
                    Description = "Điểm tham quan ven sông.",
                    TtsScript = "Một câu chuyện dài về thương cảng.",
                    Status = PoiStatus.Published,
                    Translations =
                    [
                        new TranslationDto
                        {
                            LanguageCode = "en",
                            Highlight = "Riverfront"
                        }
                    ],
                    Geofences =
                    [
                        new GeofenceDto
                        {
                            Name = "Bán kính gần bến",
                            RadiusMeters = 45
                        }
                    ]
                }
            ]
        };

        var tourResponse = new ApiResponse<IReadOnlyList<TourDto>>
        {
            Succeeded = true,
            Data =
            [
                new TourDto
                {
                    Id = 9,
                    Title = "Tour ven sông",
                    Description = "Một tuyến đi bộ ngắn.",
                    EstimatedMinutes = 30,
                    Status = TourStatus.Published,
                    Stops =
                    [
                        new TourStopDto { PoiId = 7, Sequence = 1, RadiusMeters = 60 },
                        new TourStopDto { PoiId = 8, Sequence = 2, RadiusMeters = 80 }
                    ]
                }
            ]
        };

        var locationService = new FakeVisitorLocationService(VisitorLocationSnapshot.Disabled());
        var service = CreateService(locationService, (request, cancellationToken) =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/pois")
            {
                return Task.FromResult(CreateJsonResponse(poiResponse));
            }

            if (request.RequestUri!.AbsolutePath == "/api/tours")
            {
                return Task.FromResult(CreateJsonResponse(tourResponse));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var result = await service.LoadAsync();

        Assert.False(result.IsFallback);
        Assert.Equal("Live API", result.SourceLabel);
        var poi = Assert.Single(result.Content.Pois);
        Assert.Equal("Bến Nhà Rồng", poi.Name);
        Assert.Equal("Di tích", poi.CategoryLabel);
        Assert.NotEqual(poi.CategoryLabel, poi.District);
        Assert.Contains("Live API", poi.StoryTag);
        Assert.Equal(45, poi.GeofenceRadiusMeters);
        Assert.True(poi.DistanceMeters > poi.GeofenceRadiusMeters);
        var tour = Assert.Single(result.Content.Tours);
        Assert.Equal("Tour ven sông", tour.Title);
        Assert.Equal("2 điểm dừng", tour.StopCountLabel);
        Assert.Equal(["poi-7", "poi-8"], tour.StopPoiIds);
    }

    [Fact]
    public async Task LoadAsync_FallsBackToDemoContentWhenApiFails()
    {
        var locationService = new FakeVisitorLocationService(VisitorLocationSnapshot.Disabled());
        var service = CreateService(locationService, (_, _) => throw new HttpRequestException("backend down"));

        var result = await service.LoadAsync();

        Assert.True(result.IsFallback);
        Assert.Equal("Demo fallback", result.SourceLabel);
        Assert.NotEmpty(result.Content.Pois);
        Assert.NotEmpty(result.Content.Tours);
        Assert.Contains("backend down", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_UsesNearbyEndpointWhenLocationIsAvailable()
    {
        var requestedPaths = new List<string>();
        var locationService = new FakeVisitorLocationService(
            new VisitorLocationSnapshot(
                PermissionGranted: true,
                IsLocationAvailable: true,
                Latitude: 10.7609,
                Longitude: 106.7054,
                StatusLabel: "Đã định vị"));

        var nearbyResponse = new ApiResponse<IReadOnlyList<PoiDto>>
        {
            Succeeded = true,
            Data =
            [
                new PoiDto
                {
                    Id = 11,
                    Name = "Cầu Khánh Hội",
                    Slug = "cau-khanh-hoi",
                    Lat = 10.7609,
                    Lng = 106.7054,
                    CategoryName = "Di tích",
                    Description = "POI gần vị trí hiện tại.",
                    TtsScript = "Một câu chuyện gần bạn.",
                    Status = PoiStatus.Published
                }
            ]
        };

        var tourResponse = new ApiResponse<IReadOnlyList<TourDto>>
        {
            Succeeded = true,
            Data = []
        };

        var service = CreateService(locationService, (request, cancellationToken) =>
        {
            requestedPaths.Add(request.RequestUri!.PathAndQuery);

            if (request.RequestUri!.AbsolutePath == "/api/pois/near")
            {
                return Task.FromResult(CreateJsonResponse(nearbyResponse));
            }

            if (request.RequestUri!.AbsolutePath == "/api/tours")
            {
                return Task.FromResult(CreateJsonResponse(tourResponse));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var result = await service.LoadAsync(new VisitorContentLoadRequest(PreferNearbyPois: true, RequestLocationPermission: true));

        Assert.False(result.IsFallback);
        Assert.Contains(requestedPaths, path => path.StartsWith("/api/pois/near?", StringComparison.Ordinal));
        Assert.Contains("lat=10.7609", requestedPaths[0], StringComparison.Ordinal);
        Assert.True(result.Location.PermissionGranted);
        Assert.True(result.Location.IsLocationAvailable);
    }

    [Fact]
    public async Task LoadAsync_StartsPoisAndToursRequestsWithoutWaitingForSequentialCompletion()
    {
        var startedPaths = new List<string>();
        var gate = new object();
        var locationService = new FakeVisitorLocationService(VisitorLocationSnapshot.Disabled());
        var service = CreateService(locationService, async (request, cancellationToken) =>
        {
            lock (gate)
            {
                startedPaths.Add(request.RequestUri!.AbsolutePath);
            }

            await Task.Delay(250, cancellationToken);

            return request.RequestUri!.AbsolutePath switch
            {
                "/api/pois" => CreateJsonResponse(new ApiResponse<IReadOnlyList<PoiDto>>
                {
                    Succeeded = true,
                    Data = []
                }),
                "/api/tours" => CreateJsonResponse(new ApiResponse<IReadOnlyList<TourDto>>
                {
                    Succeeded = true,
                    Data = []
                }),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        });

        var loadTask = service.LoadAsync();
        await Task.Delay(90);

        lock (gate)
        {
            Assert.Contains("/api/pois", startedPaths);
            Assert.Contains("/api/tours", startedPaths);
        }

        var result = await loadTask;
        Assert.False(result.IsFallback);
    }

    private static VisitorContentService CreateService(
        IVisitorLocationService locationService,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://10.0.2.2:5001/")
        };

        return new VisitorContentService(httpClient, locationService);
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

    private sealed class FakeVisitorLocationService(VisitorLocationSnapshot snapshot) : IVisitorLocationService
    {
        public Task<VisitorLocationSnapshot> GetCurrentAsync(bool requestPermission, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(snapshot);
        }
    }
}
