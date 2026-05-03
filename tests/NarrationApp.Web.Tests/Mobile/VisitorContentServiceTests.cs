using System.Net;
using System.Net.Http.Json;
using NarrationApp.Mobile.Features.Home;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Category;
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
                    CategoryId = 5,
                    CategoryName = "Di tích",
                    Description = "Điểm tham quan ven sông.",
                    TtsScript = "Một câu chuyện dài về thương cảng.",
                    ImageUrl = "/uploads/poi/ben-nha-rong.webp",
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

        var categoryResponse = new ApiResponse<IReadOnlyList<CategoryDto>>
        {
            Succeeded = true,
            Data =
            [
                new CategoryDto
                {
                    Id = 5,
                    Name = "Di tích",
                    Slug = "di-tich",
                    Description = "Nhóm lịch sử và di sản.",
                    Icon = "🏛️",
                    DisplayOrder = 10,
                    IsActive = true
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

            if (request.RequestUri!.AbsolutePath == "/api/categories")
            {
                return Task.FromResult(CreateJsonResponse(categoryResponse));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var result = await service.LoadAsync();

        Assert.False(result.IsFallback);
        Assert.Equal("Live API", result.SourceLabel);
        var category = Assert.Single(result.Content.Categories!);
        Assert.Equal("di-tich", category.Id);
        Assert.Equal("Di tích", category.Label);
        Assert.Equal("🏛️", category.MarkerLabel);
        var poi = Assert.Single(result.Content.Pois);
        Assert.Equal("Bến Nhà Rồng", poi.Name);
        Assert.Equal("di-tich", poi.CategoryId);
        Assert.Equal("Di tích", poi.CategoryLabel);
        Assert.Equal("https://10.0.2.2:5001/uploads/poi/ben-nha-rong.webp", poi.ImageUrl);
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
    public async Task LoadAsync_returns_empty_live_state_when_api_fails()
    {
        var locationService = new FakeVisitorLocationService(VisitorLocationSnapshot.Disabled());
        var service = CreateService(locationService, (_, _) => throw new HttpRequestException("backend down"));

        var result = await service.LoadAsync();

        Assert.True(result.IsFallback);
        Assert.Equal("API unavailable", result.SourceLabel);
        Assert.Empty(result.Content.Pois);
        Assert.Empty(result.Content.Tours);
        Assert.Empty(result.Content.Categories ?? []);
        Assert.Contains("backend down", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_ReturnsCachedContentWhenApiFailsAndOfflineSnapshotExists()
    {
        var cachedSnapshot = new VisitorContentSnapshot(
            [
                new VisitorPoi(
                    "poi-7",
                    "Bến Nhà Rồng offline",
                    "di-tich",
                    "Di tích",
                    "Quận 4",
                    "Offline cache",
                    "Nội dung POI đã lưu trên máy.",
                    "Đã cache",
                    50,
                    50,
                    0,
                    "01:30",
                    "Có audio cache",
                    10.7609,
                    106.7054,
                    ReadyAudioLanguageCodesRaw: ["vi"])
            ],
            [],
            [
                new VisitorCategory("di-tich", "Di tích", "🏛️")
            ]);
        var cacheStore = new FakeVisitorOfflineCacheStore { ContentSnapshot = cachedSnapshot };
        var locationService = new FakeVisitorLocationService(VisitorLocationSnapshot.Disabled());
        var service = CreateService(
            locationService,
            (_, _) => throw new HttpRequestException("backend down"),
            cacheStore);

        var result = await service.LoadAsync();

        Assert.True(result.IsFallback);
        Assert.Equal("Offline cache", result.SourceLabel);
        Assert.Contains("offline", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Bến Nhà Rồng offline", Assert.Single(result.Content.Pois).Name);
        Assert.Equal("Di tích", Assert.Single(result.Content.Categories!).Label);
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

        var categoryResponse = new ApiResponse<IReadOnlyList<CategoryDto>>
        {
            Succeeded = true,
            Data =
            [
                new CategoryDto
                {
                    Id = 8,
                    Name = "Di tích",
                    Slug = "di-tich",
                    Description = "Nhóm lịch sử.",
                    Icon = "🏛️",
                    DisplayOrder = 10,
                    IsActive = true
                }
            ]
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

            if (request.RequestUri!.AbsolutePath == "/api/categories")
            {
                return Task.FromResult(CreateJsonResponse(categoryResponse));
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
    public async Task LoadAsync_maps_only_ready_audio_languages_into_poi_availability()
    {
        var poiResponse = new ApiResponse<IReadOnlyList<PoiDto>>
        {
            Succeeded = true,
            Data =
            [
                new PoiDto
                {
                    Id = 17,
                    Name = "Cầu Mống",
                    Slug = "cau-mong",
                    Lat = 10.7693,
                    Lng = 106.7001,
                    CategoryName = "Di tích",
                    Description = "POI test audio language readiness.",
                    TtsScript = "Một câu chuyện về cầu Mống.",
                    Status = PoiStatus.Published
                }
            ]
        };

        var audioResponse = new ApiResponse<IReadOnlyList<AudioDto>>
        {
            Succeeded = true,
            Data =
            [
                new AudioDto
                {
                    Id = 1001,
                    PoiId = 17,
                    LanguageCode = "vi",
                    Status = AudioStatus.Ready
                },
                new AudioDto
                {
                    Id = 1002,
                    PoiId = 17,
                    LanguageCode = "en",
                    Status = AudioStatus.Ready
                },
                new AudioDto
                {
                    Id = 1003,
                    PoiId = 17,
                    LanguageCode = "ja",
                    Status = AudioStatus.Generating
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

            if (request.RequestUri!.AbsolutePath == "/api/audio")
            {
                return Task.FromResult(CreateJsonResponse(audioResponse));
            }

            if (request.RequestUri!.AbsolutePath == "/api/tours")
            {
                return Task.FromResult(CreateJsonResponse(new ApiResponse<IReadOnlyList<TourDto>> { Succeeded = true, Data = [] }));
            }

            if (request.RequestUri!.AbsolutePath == "/api/categories")
            {
                return Task.FromResult(CreateJsonResponse(new ApiResponse<IReadOnlyList<CategoryDto>> { Succeeded = true, Data = [] }));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var result = await service.LoadAsync();

        var poi = Assert.Single(result.Content.Pois);
        Assert.Equal(["en", "vi"], poi.ReadyAudioLanguageCodes.OrderBy(code => code));
        Assert.Equal(2, poi.AvailableLanguageCount);
    }

    [Fact]
    public void Map_ProjectsRealDistanceMetersFromCurrentLocation()
    {
        IReadOnlyList<PoiDto> pois =
        [
            new PoiDto
            {
                Id = 21,
                Name = "Ốc Oanh",
                Slug = "oc-oanh-vinh-khanh",
                Lat = 10.7607,
                Lng = 106.7033,
                CategoryName = "Hải sản",
                Description = "POI kiểm tra khoảng cách thật.",
                TtsScript = "Audio demo",
                Status = PoiStatus.Published
            }
        ];

        var location = new VisitorLocationSnapshot(
            PermissionGranted: true,
            IsLocationAvailable: true,
            Latitude: 10.7607,
            Longitude: 106.7033,
            StatusLabel: "GPS live");

        var snapshot = VisitorContentMapper.Map(pois, [], [], location);

        var poi = Assert.Single(snapshot.Pois);
        Assert.Equal(0, poi.DistanceMeters);
    }

    [Fact]
    public void Map_ResolvesRelativeImageUrlsAgainstApiBaseAddress()
    {
        IReadOnlyList<PoiDto> pois =
        [
            new PoiDto
            {
                Id = 31,
                Name = "Cầu Khánh Hội",
                Slug = "cau-khanh-hoi",
                Lat = 10.7609,
                Lng = 106.7054,
                CategoryName = "Di tích",
                Description = "POI có ảnh tương đối từ API.",
                TtsScript = "Audio demo",
                ImageUrl = "uploads/poi/cau-khanh-hoi.webp",
                Status = PoiStatus.Published
            }
        ];

        var snapshot = VisitorContentMapper.Map(
            pois,
            [],
            [],
            VisitorLocationSnapshot.Disabled(),
            new Uri("https://visitor.example/"));

        var poi = Assert.Single(snapshot.Pois);
        Assert.Equal("https://visitor.example/uploads/poi/cau-khanh-hoi.webp", poi.ImageUrl);
    }

    [Fact]
    public async Task LoadAsync_FallsBackToAllPoisWhenNearbyEndpointReturnsEmpty()
    {
        var requestedPaths = new List<string>();
        var locationService = new FakeVisitorLocationService(
            new VisitorLocationSnapshot(
                PermissionGranted: true,
                IsLocationAvailable: true,
                Latitude: 37.4220,
                Longitude: -122.0840,
                StatusLabel: "Đã định vị"));

        var nearbyResponse = new ApiResponse<IReadOnlyList<PoiDto>>
        {
            Succeeded = true,
            Data = []
        };

        var allPoisResponse = new ApiResponse<IReadOnlyList<PoiDto>>
        {
            Succeeded = true,
            Data =
            [
                new PoiDto
                {
                    Id = 14,
                    Name = "Ốc Oanh",
                    Slug = "oc-oanh-vinh-khanh",
                    Lat = 10.7607,
                    Lng = 106.7033,
                    CategoryId = 1,
                    CategoryName = "Hải sản",
                    Description = "Quán ốc lâu năm.",
                    TtsScript = "Một câu chuyện về ốc Oanh.",
                    Status = PoiStatus.Published
                }
            ]
        };

        var categoryResponse = new ApiResponse<IReadOnlyList<CategoryDto>>
        {
            Succeeded = true,
            Data =
            [
                new CategoryDto
                {
                    Id = 1,
                    Name = "Hải sản",
                    Slug = "hai-san",
                    Description = "Nhóm hải sản.",
                    Icon = "🦐",
                    DisplayOrder = 10,
                    IsActive = true
                }
            ]
        };

        var service = CreateService(locationService, (request, cancellationToken) =>
        {
            requestedPaths.Add(request.RequestUri!.PathAndQuery);

            if (request.RequestUri!.AbsolutePath == "/api/pois/near")
            {
                return Task.FromResult(CreateJsonResponse(nearbyResponse));
            }

            if (request.RequestUri!.AbsolutePath == "/api/pois")
            {
                return Task.FromResult(CreateJsonResponse(allPoisResponse));
            }

            if (request.RequestUri!.AbsolutePath == "/api/tours")
            {
                return Task.FromResult(CreateJsonResponse(new ApiResponse<IReadOnlyList<TourDto>> { Succeeded = true, Data = [] }));
            }

            if (request.RequestUri!.AbsolutePath == "/api/categories")
            {
                return Task.FromResult(CreateJsonResponse(categoryResponse));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var result = await service.LoadAsync(new VisitorContentLoadRequest(PreferNearbyPois: true, RequestLocationPermission: true));

        Assert.False(result.IsFallback);
        Assert.Contains(requestedPaths, path => path.StartsWith("/api/pois/near?", StringComparison.Ordinal));
        Assert.Contains("/api/pois", requestedPaths);
        Assert.Single(result.Content.Pois);
        Assert.Equal("Ốc Oanh", result.Content.Pois[0].Name);
        Assert.Contains("toàn bộ", result.Message, StringComparison.OrdinalIgnoreCase);
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
                "/api/categories" => CreateJsonResponse(new ApiResponse<IReadOnlyList<CategoryDto>>
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
            Assert.Contains("/api/categories", startedPaths);
        }

        var result = await loadTask;
        Assert.False(result.IsFallback);
    }

    private static VisitorContentService CreateService(
        IVisitorLocationService locationService,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler,
        IVisitorOfflineCacheStore? offlineCacheStore = null)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://10.0.2.2:5001/")
        };

        return new VisitorContentService(
            httpClient,
            locationService,
            offlineCacheStore ?? new FakeVisitorOfflineCacheStore());
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
