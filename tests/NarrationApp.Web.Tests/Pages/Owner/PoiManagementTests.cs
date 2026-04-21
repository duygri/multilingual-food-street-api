using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Shared.DTOs.Geofence;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Owner;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class PoiManagementTests : TestContext
{
    [Fact]
    public void Update_poi_content_fields_without_standalone_audio_lane()
    {
        var ownerService = new TestOwnerPortalService();
        var audioService = new TestAudioPortalService();
        var moderationService = new TestModerationPortalService();
        var geofenceService = new TestGeofencePortalService();
        var categoryService = new TestCategoryPortalService();

        Services.AddSingleton<IOwnerPortalService>(ownerService);
        Services.AddSingleton<IAudioPortalService>(audioService);
        Services.AddSingleton<IModerationPortalService>(moderationService);
        Services.AddSingleton<IGeofencePortalService>(geofenceService);
        Services.AddSingleton<ICategoryPortalService>(categoryService);

        var cut = RenderComponent<PoiManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Bàn điều phối POI", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.Contains("128", cut.Markup);
            Assert.Contains("Trình biên tập POI", cut.Markup);
            Assert.Contains("Trạng thái POI", cut.Markup);
            Assert.DoesNotContain("Điều phối POI và geofence", cut.Markup);
            Assert.DoesNotContain("Lane audio", cut.Markup);
            Assert.DoesNotContain("tts-vi", cut.Markup);
            Assert.DoesNotContain("Owned POIs", cut.Markup);
            Assert.DoesNotContain("POI editor", cut.Markup);
            Assert.DoesNotContain("Owner POI", cut.Markup);
        });

        cut.Find("select[data-field='poi-category']").Change("2");
        cut.Find("textarea[data-field='poi-description']").Change("Mô tả đã được owner cập nhật.");
        cut.Find("textarea[data-field='poi-tts-script']").Change("Kịch bản tiếng Việt để admin dùng tạo audio gốc.");
        cut.Find("button[data-action='save-poi']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã cập nhật POI", cut.Markup);
            Assert.Contains("Mô tả đã được owner cập nhật.", cut.Markup);
        });

        Assert.Single(ownerService.UpdateRequests);
        Assert.Equal(2, ownerService.UpdateRequests[0].CategoryId);
        Assert.Equal("Mô tả đã được owner cập nhật.", ownerService.UpdateRequests[0].Description);
        Assert.Equal("Kịch bản tiếng Việt để admin dùng tạo audio gốc.", ownerService.UpdateRequests[0].TtsScript);
    }

    [Fact]
    public void Request_review_creates_new_pending_moderation_entry()
    {
        var ownerService = new TestOwnerPortalService();
        var audioService = new TestAudioPortalService();
        var moderationService = new TestModerationPortalService();
        var geofenceService = new TestGeofencePortalService();
        var categoryService = new TestCategoryPortalService();

        Services.AddSingleton<IOwnerPortalService>(ownerService);
        Services.AddSingleton<IAudioPortalService>(audioService);
        Services.AddSingleton<IModerationPortalService>(moderationService);
        Services.AddSingleton<IGeofencePortalService>(geofenceService);
        Services.AddSingleton<ICategoryPortalService>(categoryService);

        var cut = RenderComponent<PoiManagement>();

        cut.WaitForAssertion(() => Assert.Contains("Chưa có yêu cầu kiểm duyệt", cut.Markup));
        cut.Find("button[data-action='request-review']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("POI #1", cut.Markup);
            Assert.Contains("Chờ duyệt", cut.Markup);
            Assert.DoesNotContain("Pending", cut.Markup);
        });

        Assert.Single(moderationService.CreatedRequests);
        Assert.Equal("poi", moderationService.CreatedRequests[0].EntityType);
        Assert.Equal("1", moderationService.CreatedRequests[0].EntityId);
    }

    [Fact]
    public void Create_new_poi_adds_it_to_owner_list_and_selects_it()
    {
        var ownerService = new TestOwnerPortalService();
        var audioService = new TestAudioPortalService();
        var moderationService = new TestModerationPortalService();
        var geofenceService = new TestGeofencePortalService();
        var categoryService = new TestCategoryPortalService();

        Services.AddSingleton<IOwnerPortalService>(ownerService);
        Services.AddSingleton<IAudioPortalService>(audioService);
        Services.AddSingleton<IModerationPortalService>(moderationService);
        Services.AddSingleton<IGeofencePortalService>(geofenceService);
        Services.AddSingleton<ICategoryPortalService>(categoryService);

        var cut = RenderComponent<PoiManagement>();

        cut.WaitForAssertion(() => Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup));
        cut.Find("button[data-action='new-poi']").Click();
        cut.Find("input[data-field='poi-name']").Change("Ốc đêm Vĩnh Khánh");
        cut.Find("input[data-field='poi-slug']").Change("oc-dem-vinh-khanh");
        cut.Find("input[data-field='poi-lat']").Change("10.7605");
        cut.Find("input[data-field='poi-lng']").Change("106.7041");
        cut.Find("input[data-field='poi-priority']").Change("18");
        cut.Find("select[data-field='poi-category']").Change("3");
        cut.Find("textarea[data-field='poi-description']").Change("Điểm ăn đêm nhiều khách quen.");
        cut.Find("textarea[data-field='poi-tts-script']").Change("Kịch bản source để admin tạo audio.");
        cut.Find("button[data-action='save-poi']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Ốc đêm Vĩnh Khánh", cut.Markup);
            Assert.Contains("Đã tạo POI mới", cut.Markup);
        });

        Assert.Single(ownerService.CreateRequests);
        Assert.Equal("Ốc đêm Vĩnh Khánh", ownerService.CreateRequests[0].Name);
        Assert.Equal(3, ownerService.CreateRequests[0].CategoryId);
    }

    [Fact]
    public void Update_and_delete_selected_poi_changes_owner_list()
    {
        var ownerService = new TestOwnerPortalService();
        var audioService = new TestAudioPortalService();
        var moderationService = new TestModerationPortalService();
        var geofenceService = new TestGeofencePortalService();
        var categoryService = new TestCategoryPortalService();

        Services.AddSingleton<IOwnerPortalService>(ownerService);
        Services.AddSingleton<IAudioPortalService>(audioService);
        Services.AddSingleton<IModerationPortalService>(moderationService);
        Services.AddSingleton<IGeofencePortalService>(geofenceService);
        Services.AddSingleton<ICategoryPortalService>(categoryService);

        var cut = RenderComponent<PoiManagement>();

        cut.WaitForAssertion(() => Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup));
        cut.Find("input[data-field='poi-name']").Change("Bún mắm Vĩnh Khánh Premium");
        cut.Find("button[data-action='save-poi']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Bún mắm Vĩnh Khánh Premium", cut.Markup);
            Assert.Contains("Đã cập nhật POI", cut.Markup);
        });

        cut.Find("button[data-action='delete-poi']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Bún mắm Vĩnh Khánh Premium", cut.Markup);
            Assert.Contains("Tạo POI mới", cut.Markup);
        });

        Assert.Single(ownerService.UpdateRequests);
        Assert.Single(ownerService.DeleteRequests);
        Assert.Equal("Bún mắm Vĩnh Khánh Premium", ownerService.UpdateRequests[0].Name);
    }

    [Fact]
    public void Save_geofence_updates_primary_geofence_for_selected_poi()
    {
        var ownerService = new TestOwnerPortalService();
        var audioService = new TestAudioPortalService();
        var moderationService = new TestModerationPortalService();
        var geofenceService = new TestGeofencePortalService();
        var categoryService = new TestCategoryPortalService();

        Services.AddSingleton<IOwnerPortalService>(ownerService);
        Services.AddSingleton<IAudioPortalService>(audioService);
        Services.AddSingleton<IModerationPortalService>(moderationService);
        Services.AddSingleton<IGeofencePortalService>(geofenceService);
        Services.AddSingleton<ICategoryPortalService>(categoryService);

        var cut = RenderComponent<PoiManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Vùng kích hoạt mặc định", cut.Markup);
            Assert.Contains("Vùng kích hoạt chính", cut.Markup);
            Assert.Contains("35", cut.Markup);
            Assert.DoesNotContain("Primary geofence", cut.Markup);
        });

        cut.Find("input[data-field='geofence-name']").Change("Vùng quảng trường chính");
        cut.Find("input[data-field='geofence-radius']").Change("42");
        cut.Find("input[data-field='geofence-priority']").Change("11");
        cut.Find("input[data-field='geofence-debounce']").Change("14");
        cut.Find("input[data-field='geofence-cooldown']").Change("900");
        cut.Find("input[data-field='geofence-trigger']").Change("notify_only");
        cut.Find("input[data-field='geofence-nearest-only']").Change(false);
        cut.Find("input[data-field='geofence-active']").Change(false);
        cut.Find("button[data-action='save-geofence']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã cập nhật vùng kích hoạt", cut.Markup);
            Assert.Contains("Vùng quảng trường chính", cut.Markup);
            Assert.Contains("42", cut.Markup);
        });

        Assert.Single(geofenceService.UpdateRequests);
        Assert.Equal("Vùng quảng trường chính", geofenceService.UpdateRequests[0].Name);
        Assert.Equal(42, geofenceService.UpdateRequests[0].RadiusMeters);
        Assert.False(geofenceService.UpdateRequests[0].IsActive);
        Assert.False(geofenceService.UpdateRequests[0].NearestOnly);
    }

    private sealed class TestOwnerPortalService : IOwnerPortalService
    {
        private readonly Guid _ownerId = Guid.NewGuid();
        private readonly List<PoiDto> _pois;

        public TestOwnerPortalService()
        {
            _pois =
            [
                new PoiDto
                {
                    Id = 1,
                    Name = "Bún mắm Vĩnh Khánh",
                    Slug = "bun-mam-vinh-khanh",
                    OwnerId = _ownerId,
                    Lat = 10.758d,
                    Lng = 106.701d,
                    Priority = 10,
                    CategoryId = 1,
                    CategoryName = "Hải sản",
                    NarrationMode = NarrationMode.TtsOnly,
                    Status = PoiStatus.Draft,
                    Description = "Món bún mắm đậm vị về đêm.",
                    TtsScript = "Kịch bản gốc tiếng Việt cho POI.",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                    Translations =
                    [
                        new TranslationDto { Id = 101, PoiId = 1, LanguageCode = "vi", Title = "Bún mắm", Description = "Đậm vị" }
                    ],
                    Geofences =
                    [
                        new GeofenceDto
                        {
                            Id = 401,
                            PoiId = 1,
                            Name = "Vùng kích hoạt chính",
                            RadiusMeters = 35,
                            Priority = 8,
                            DebounceSeconds = 10,
                            CooldownSeconds = 600,
                            IsActive = true,
                            TriggerAction = "auto_play",
                            NearestOnly = true
                        }
                    ]
                }
            ];
        }

        public List<CreatePoiRequest> CreateRequests { get; } = [];

        public List<UpdatePoiRequest> UpdateRequests { get; } = [];

        public List<int> DeleteRequests { get; } = [];

        public Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OwnerDashboardDto());
        }

        public Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PoiDto>>(_pois.ToArray());
        }

        public Task<OwnerPoiStatsDto> GetPoiStatsAsync(int poiId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OwnerPoiStatsDto
            {
                PoiId = poiId,
                TotalVisits = 128,
                AudioPlays = 64,
                TranslationCount = 1,
                AudioAssetCount = 0,
                GeofenceCount = 2
            });
        }

        public Task<PoiDto> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default)
        {
            CreateRequests.Add(request);

            var created = new PoiDto
            {
                Id = _pois.Max(item => item.Id) + 1,
                Name = request.Name,
                Slug = request.Slug,
                OwnerId = _ownerId,
                Lat = request.Lat,
                Lng = request.Lng,
                Priority = request.Priority,
                CategoryId = request.CategoryId,
                CategoryName = request.CategoryId switch
                {
                    1 => "Hải sản",
                    2 => "Bún/Phở",
                    3 => "Ăn vặt",
                    _ => null
                },
                NarrationMode = request.NarrationMode,
                MapLink = request.MapLink,
                ImageUrl = request.ImageUrl,
                Description = request.Description,
                TtsScript = request.TtsScript,
                Status = PoiStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow
            };

            _pois.Insert(0, created);
            return Task.FromResult(created);
        }

        public Task<PoiDto> UpdatePoiAsync(int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default)
        {
            UpdateRequests.Add(request);

            var existing = _pois.Single(item => item.Id == poiId);
            var updated = new PoiDto
            {
                Id = existing.Id,
                Name = request.Name,
                Slug = request.Slug,
                OwnerId = existing.OwnerId,
                Lat = request.Lat,
                Lng = request.Lng,
                Priority = request.Priority,
                CategoryId = request.CategoryId,
                CategoryName = request.CategoryId switch
                {
                    1 => "Hải sản",
                    2 => "Bún/Phở",
                    3 => "Ăn vặt",
                    _ => existing.CategoryName
                },
                NarrationMode = request.NarrationMode,
                MapLink = request.MapLink,
                ImageUrl = request.ImageUrl,
                Description = request.Description,
                TtsScript = request.TtsScript,
                Status = PoiStatus.Updated,
                CreatedAtUtc = existing.CreatedAtUtc,
                Translations = existing.Translations,
                Geofences = existing.Geofences
            };

            _pois[_pois.FindIndex(item => item.Id == poiId)] = updated;
            return Task.FromResult(updated);
        }

        public Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default)
        {
            DeleteRequests.Add(poiId);
            _pois.RemoveAll(item => item.Id == poiId);
            return Task.CompletedTask;
        }
    }

    private sealed class TestAudioPortalService : IAudioPortalService
    {
        public Task<IReadOnlyList<AudioDto>> GetByPoiAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AudioDto>>(Array.Empty<AudioDto>());
        }

        public Task<AudioDto> GenerateTtsAsync(TtsGenerateRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AudioDto> GenerateFromTranslationAsync(GenerateAudioFromTranslationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AudioDto> UploadAsync(UploadAudioRequest request, Stream content, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AudioDto> UpdateAsync(int audioId, UpdateAudioRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(int audioId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestModerationPortalService : IModerationPortalService
    {
        private readonly List<ModerationRequestDto> _items = [];

        public List<CreateModerationRequest> CreatedRequests { get; } = [];

        public Task<IReadOnlyList<ModerationRequestDto>> GetMineAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ModerationRequestDto>>(_items.ToArray());
        }

        public Task<ModerationRequestDto> CreateAsync(CreateModerationRequest request, CancellationToken cancellationToken = default)
        {
            CreatedRequests.Add(request);

            var item = new ModerationRequestDto
            {
                Id = 301,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                RequestedBy = Guid.NewGuid(),
                Status = ModerationStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            _items.Insert(0, item);
            return Task.FromResult(item);
        }
    }

    private sealed class TestGeofencePortalService : IGeofencePortalService
    {
        public List<UpdateGeofenceRequest> UpdateRequests { get; } = [];

        public Task<GeofenceDto> UpdateAsync(int poiId, UpdateGeofenceRequest request, CancellationToken cancellationToken = default)
        {
            UpdateRequests.Add(request);

            return Task.FromResult(new GeofenceDto
            {
                Id = 401,
                PoiId = poiId,
                Name = request.Name,
                RadiusMeters = request.RadiusMeters,
                Priority = request.Priority,
                DebounceSeconds = request.DebounceSeconds,
                CooldownSeconds = request.CooldownSeconds,
                IsActive = request.IsActive,
                TriggerAction = request.TriggerAction,
                NearestOnly = request.NearestOnly
            });
        }
    }

    private sealed class TestCategoryPortalService : ICategoryPortalService
    {
        public Task<IReadOnlyList<CategoryDto>> GetAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CategoryDto>>(
            [
                new CategoryDto { Id = 1, Name = "Hải sản", Slug = "hai-san", Description = "Nhóm hải sản", Icon = "🦐", DisplayOrder = 1, IsActive = true },
                new CategoryDto { Id = 2, Name = "Bún/Phở", Slug = "bun-pho", Description = "Nhóm bún phở", Icon = "🍜", DisplayOrder = 2, IsActive = true },
                new CategoryDto { Id = 3, Name = "Ăn vặt", Slug = "an-vat", Description = "Nhóm ăn vặt", Icon = "🥟", DisplayOrder = 3, IsActive = true }
            ]);
        }

        public Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
