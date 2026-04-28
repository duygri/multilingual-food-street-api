using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
using System.Net;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class PoiDetailTests : TestContext
{
    [Fact]
    public void Detail_page_behavior_is_split_into_focused_partials()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pageRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Owner");
        var markupPath = Path.Combine(pageRoot, "PoiDetail.razor");
        var expectedPartials = new[]
        {
            ("PoiDetail.razor.cs", "OnParametersSetAsync"),
            ("PoiDetail.PoiActions.razor.cs", "SavePoiAsync"),
            ("PoiDetail.Uploads.razor.cs", "UploadSourceAudioAsync"),
            ("PoiDetail.Moderation.razor.cs", "RequestReviewAsync"),
            ("PoiDetail.Presentation.razor.cs", "GetPoiStatusLabel"),
            ("PoiDetail.EditorModels.razor.cs", "PoiEditModel")
        };

        var markup = File.ReadAllText(markupPath);
        Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);

        foreach (var (fileName, marker) in expectedPartials)
        {
            var path = Path.Combine(pageRoot, fileName);
            Assert.True(File.Exists(path), $"{fileName} should exist.");
            var source = File.ReadAllText(path);
            Assert.Contains("partial class PoiDetail", source, StringComparison.Ordinal);
            Assert.Contains(marker, source, StringComparison.Ordinal);
        }

        var coreLines = File.ReadAllLines(Path.Combine(pageRoot, "PoiDetail.razor.cs")).Length;
        Assert.True(coreLines <= 100, $"PoiDetail.razor.cs should stay focused on state/load orchestration, but has {coreLines} lines.");
        var poiActionLines = File.ReadAllLines(Path.Combine(pageRoot, "PoiDetail.PoiActions.razor.cs")).Length;
        Assert.True(poiActionLines <= 140, $"PoiDetail.PoiActions.razor.cs should stay focused on poi/geofence actions, but has {poiActionLines} lines.");
        var uploadLines = File.ReadAllLines(Path.Combine(pageRoot, "PoiDetail.Uploads.razor.cs")).Length;
        Assert.True(uploadLines <= 120, $"PoiDetail.Uploads.razor.cs should stay focused on upload flows, but has {uploadLines} lines.");
        var moderationLines = File.ReadAllLines(Path.Combine(pageRoot, "PoiDetail.Moderation.razor.cs")).Length;
        Assert.True(moderationLines <= 90, $"PoiDetail.Moderation.razor.cs should stay focused on moderation state and actions, but has {moderationLines} lines.");
        var presentationLines = File.ReadAllLines(Path.Combine(pageRoot, "PoiDetail.Presentation.razor.cs")).Length;
        Assert.True(presentationLines <= 130, $"PoiDetail.Presentation.razor.cs should stay focused on view helpers, but has {presentationLines} lines.");
        var editorLines = File.ReadAllLines(Path.Combine(pageRoot, "PoiDetail.EditorModels.razor.cs")).Length;
        Assert.True(editorLines <= 120, $"PoiDetail.EditorModels.razor.cs should stay focused on editor mapping, but has {editorLines} lines.");
    }

    [Fact]
    public void Detail_page_renders_preview_stats_rejection_surface_and_audio_table()
    {
        ConfigureDetail();

        var cut = RenderComponent<PoiDetail>(parameters => parameters.Add(page => page.Id, 1));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Chi tiết POI", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.Contains("Bị từ chối", cut.Markup);
            Assert.Contains("Ảnh minh họa", cut.Markup);
            Assert.Contains("Hải sản", cut.Markup);
            Assert.Contains("10.758", cut.Markup);
            Assert.Contains("35m", cut.Markup);
            Assert.Contains("POI #1", cut.Markup);
            Assert.Contains("Lượt ghé", cut.Markup);
            Assert.Contains("128", cut.Markup);
            Assert.Contains("Audio plays", cut.Markup);
            Assert.Contains("64", cut.Markup);
            Assert.Contains("Cần chỉnh trước khi gửi lại", cut.Markup);
            Assert.Contains("Thiếu mô tả nguồn rõ ràng.", cut.Markup);
            Assert.Contains("Audio đa ngôn ngữ", cut.Markup);
            Assert.Contains("vi", cut.Markup);
            Assert.Contains("en", cut.Markup);
            Assert.Contains("Ready", cut.Markup);
            Assert.Contains("Generating", cut.Markup);
            Assert.DoesNotContain("URL hình ảnh", cut.Markup);
        });

        Assert.Equal("Sửa & gửi lại", cut.Find("button[data-action='request-review-rejected']").TextContent.Trim());
    }

    [Fact]
    public void Detail_page_preserves_editing_geofence_upload_review_and_delete_flows()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var (ownerService, audioService, moderationService, geofenceService, navigation) = ConfigureDetail();

        var cut = RenderComponent<PoiDetail>(parameters => parameters.Add(page => page.Id, 1));

        cut.WaitForAssertion(() => Assert.Contains("Thông tin chỉnh sửa", cut.Markup));

        cut.Find("input[data-field='poi-name']").Change("Bún mắm đã chỉnh");
        cut.Find("input[data-field='poi-slug']").Change("bun-mam-da-chinh");
        cut.Find("input[data-field='poi-lat']").Change("10.759");
        cut.Find("input[data-field='poi-lng']").Change("106.702");
        cut.Find("input[data-field='poi-priority']").Change("12");
        cut.Find("select[data-field='poi-category']").Change("3");
        cut.Find("select[data-field='poi-mode']").Change(NarrationMode.RecordedOnly.ToString());
        cut.Find("input[data-field='poi-map-link']").Change("https://maps.test/poi-1");
        cut.Find("textarea[data-field='poi-description']").Change("Mô tả đã bổ sung cho admin.");
        cut.Find("textarea[data-field='poi-tts-script']").Change("Kịch bản TTS đã chỉnh.");
        cut.Find("button[data-action='save-poi']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(ownerService.UpdateRequests);
            Assert.Contains("Đã lưu Bún mắm đã chỉnh.", cut.Markup);
        });

        var updateRequest = ownerService.UpdateRequests[0];
        Assert.Equal("Bún mắm đã chỉnh", updateRequest.Name);
        Assert.Equal("bun-mam-da-chinh", updateRequest.Slug);
        Assert.Equal(10.759, updateRequest.Lat);
        Assert.Equal(106.702, updateRequest.Lng);
        Assert.Equal(12, updateRequest.Priority);
        Assert.Equal(3, updateRequest.CategoryId);
        Assert.Equal(NarrationMode.RecordedOnly, updateRequest.NarrationMode);
        Assert.Equal("https://maps.test/poi-1", updateRequest.MapLink);
        Assert.Null(updateRequest.ImageUrl);
        Assert.Equal("Mô tả đã bổ sung cho admin.", updateRequest.Description);
        Assert.Equal("Kịch bản TTS đã chỉnh.", updateRequest.TtsScript);

        cut.Find("input[data-field='geofence-name']").Change("Vùng kích hoạt đã chỉnh");
        cut.Find("input[data-field='geofence-radius']").Change("55");
        cut.Find("input[data-field='geofence-priority']").Change("4");
        cut.Find("input[data-field='geofence-debounce']").Change("6");
        cut.Find("input[data-field='geofence-cooldown']").Change("240");
        cut.Find("input[data-field='geofence-trigger']").Change("manual_preview");
        cut.Find("input[data-field='geofence-active']").Change(false);
        cut.Find("input[data-field='geofence-nearest-only']").Change(false);
        cut.Find("button[data-action='save-geofence']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(geofenceService.UpdateRequests);
            Assert.Contains("Đã cập nhật vùng kích hoạt.", cut.Markup);
        });

        var geofenceRequest = geofenceService.UpdateRequests[0];
        Assert.Equal("Vùng kích hoạt đã chỉnh", geofenceRequest.Name);
        Assert.Equal(55, geofenceRequest.RadiusMeters);
        Assert.Equal(4, geofenceRequest.Priority);
        Assert.Equal(6, geofenceRequest.DebounceSeconds);
        Assert.Equal(240, geofenceRequest.CooldownSeconds);
        Assert.Equal("manual_preview", geofenceRequest.TriggerAction);
        Assert.False(geofenceRequest.IsActive);
        Assert.False(geofenceRequest.NearestOnly);

        FindInputFile(cut, "poi-image-file").UploadFiles(
            InputFileContent.CreateFromText("representative image", "poi-hero.png", contentType: "image/png"));
        cut.Find("button[data-action='upload-poi-image']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(ownerService.UploadImageRequests);
            Assert.Contains("Đã cập nhật ảnh đại diện POI.", cut.Markup);
        });

        Assert.Equal(1, ownerService.UploadImageRequests[0].PoiId);
        Assert.Equal("poi-hero.png", ownerService.UploadImageRequests[0].FileName);
        Assert.Equal("image/png", ownerService.UploadImageRequests[0].ContentType);

        FindInputFile(cut, "source-audio").UploadFiles(
            InputFileContent.CreateFromText("recorded audio", "source-vi.mp3", contentType: "audio/mpeg"));
        cut.Find("button[data-action='upload-source-audio']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(audioService.UploadRequests);
            Assert.Contains("Đã tải audio nguồn tiếng Việt.", cut.Markup);
        });

        Assert.Equal(1, audioService.UploadRequests[0].PoiId);
        Assert.Equal("vi", audioService.UploadRequests[0].LanguageCode);
        Assert.Equal("source-vi.mp3", audioService.UploadRequests[0].FileName);

        cut.Find("button[data-action='request-review']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(moderationService.CreatedRequests);
            Assert.Contains("Đã gửi Bún mắm đã chỉnh vào hàng chờ duyệt.", cut.Markup);
        });

        cut.Find("button[data-action='open-delete-poi']").Click();
        cut.WaitForAssertion(() => Assert.Contains("Xóa POI này?", cut.Markup));
        cut.Find("button[data-action='confirm']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal([1], ownerService.DeleteRequests);
            Assert.Equal("http://localhost/owner/pois", navigation.Uri);
        });
    }

    [Fact]
    public void Detail_page_clears_stale_error_when_route_parameter_loads_successfully()
    {
        var (ownerService, _, _, _, _) = ConfigureDetail();
        ownerService.MissingPoiIds.Add(404);

        var cut = RenderComponent<PoiDetail>(parameters => parameters.Add(page => page.Id, 404));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Không thể tải chi tiết POI", cut.Markup);
            Assert.Contains("Không tìm thấy POI", cut.Markup);
        });

        cut.SetParametersAndRender(parameters => parameters.Add(page => page.Id, 1));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.DoesNotContain("Không thể tải chi tiết POI", cut.Markup);
            Assert.DoesNotContain("Không tìm thấy POI", cut.Markup);
        });
    }

    [Fact]
    public void Detail_page_ignores_historical_rejection_when_latest_moderation_is_not_rejected()
    {
        var (ownerService, _, moderationService, _, _) = ConfigureDetail();
        ownerService.Status = PoiStatus.Published;
        moderationService.Items =
        [
            new ModerationRequestDto
            {
                Id = 301,
                EntityType = "poi",
                EntityId = "1",
                Status = ModerationStatus.Rejected,
                RequestedBy = Guid.NewGuid(),
                ReviewNote = "Ghi chú từ chối cũ.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
            },
            new ModerationRequestDto
            {
                Id = 302,
                EntityType = "poi",
                EntityId = "1",
                Status = ModerationStatus.Approved,
                RequestedBy = Guid.NewGuid(),
                ReviewNote = "Đã duyệt lại.",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-1)
            }
        ];

        var cut = RenderComponent<PoiDetail>(parameters => parameters.Add(page => page.Id, 1));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.DoesNotContain("Cần chỉnh trước khi gửi lại", cut.Markup);
            Assert.Empty(cut.FindAll("button[data-action='request-review-rejected']"));
        });
    }

    [Fact]
    public void Owner_portal_get_poi_contract_requires_implementers()
    {
        var method = typeof(IOwnerPortalService).GetMethod(
            nameof(IOwnerPortalService.GetPoiAsync),
            [typeof(int), typeof(CancellationToken)]);

        Assert.NotNull(method);
        Assert.True(method.IsAbstract);
    }

    private static IRenderedComponent<InputFile> FindInputFile(IRenderedComponent<PoiDetail> cut, string dataField)
    {
        return cut.FindComponents<InputFile>()
            .Single(component => string.Equals(component.Find("input").GetAttribute("data-field"), dataField, StringComparison.Ordinal));
    }

    private (TestOwnerPortalService OwnerService,
        TestAudioPortalService AudioService,
        TestModerationPortalService ModerationService,
        TestGeofencePortalService GeofenceService,
        NavigationManager Navigation) ConfigureDetail()
    {
        var ownerService = new TestOwnerPortalService();
        var audioService = new TestAudioPortalService();
        var moderationService = new TestModerationPortalService();
        var geofenceService = new TestGeofencePortalService();

        Services.AddSingleton<IOwnerPortalService>(ownerService);
        Services.AddSingleton<IAudioPortalService>(audioService);
        Services.AddSingleton<IModerationPortalService>(moderationService);
        Services.AddSingleton<IGeofencePortalService>(geofenceService);
        Services.AddSingleton<ICategoryPortalService>(new TestCategoryPortalService());

        return (ownerService, audioService, moderationService, geofenceService, Services.GetRequiredService<NavigationManager>());
    }

    private sealed class TestOwnerPortalService : IOwnerPortalService
    {
        private PoiDto _poi = BuildPoi();
        private PoiStatus _status = PoiStatus.Rejected;

        public PoiStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                _poi = BuildPoi(status: value);
            }
        }

        public HashSet<int> MissingPoiIds { get; } = [];

        public List<UpdatePoiRequest> UpdateRequests { get; } = [];

        public List<ImageUploadCall> UploadImageRequests { get; } = [];

        public List<int> DeleteRequests { get; } = [];

        public Task<OwnerShellSummaryDto> GetShellSummaryAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OwnerShellSummaryDto());
        }

        public Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OwnerDashboardDto());
        }

        public Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PoiDto>>([_poi]);
        }

        public Task<PoiDto> GetPoiAsync(int poiId, CancellationToken cancellationToken = default)
        {
            if (MissingPoiIds.Contains(poiId))
            {
                throw new ApiException("Không tìm thấy POI", HttpStatusCode.NotFound);
            }

            return Task.FromResult(_poi);
        }

        public Task<OwnerPoiStatsDto> GetPoiStatsAsync(int poiId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OwnerPoiStatsDto
            {
                PoiId = poiId,
                TotalVisits = 128,
                AudioPlays = 64,
                TranslationCount = 2,
                AudioAssetCount = 2,
                GeofenceCount = 1
            });
        }

        public Task<OwnerPoiDetailWorkspaceDto> GetPoiWorkspaceAsync(int poiId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OwnerPoiDetailWorkspaceDto
            {
                Summary = new OwnerPoiDetailSummaryDto
                {
                    PoiId = _poi.Id,
                    PoiName = _poi.Name,
                    ImageUrl = _poi.ImageUrl,
                    Status = _poi.Status,
                    CategoryName = _poi.CategoryName
                },
                Metrics = new OwnerPoiDetailMetricsDto
                {
                    TotalVisits = 128,
                    AudioPlays = 64,
                    TranslationCount = 2,
                    AudioAssetCount = 2,
                    GeofenceCount = 1,
                    QrScans = 18,
                    TotalListenDurationSeconds = 5420
                }
            });
        }

        public Task<PoiDto> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<PoiDto> UpdatePoiAsync(int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default)
        {
            UpdateRequests.Add(request);
            _poi = BuildPoi(
                request.Name,
                request.Slug,
                request.Lat,
                request.Lng,
                request.Priority,
                request.NarrationMode,
                request.Description,
                request.TtsScript,
                request.MapLink,
                request.ImageUrl,
                request.Status);
            _status = request.Status;

            return Task.FromResult(_poi);
        }

        public Task<PoiDto> UploadPoiImageAsync(int poiId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
        {
            UploadImageRequests.Add(new ImageUploadCall(poiId, fileName, contentType));
            _poi = BuildPoi(
                _poi.Name,
                _poi.Slug,
                _poi.Lat,
                _poi.Lng,
                _poi.Priority,
                _poi.NarrationMode,
                _poi.Description,
                _poi.TtsScript,
                _poi.MapLink,
                $"https://cdn.test/{fileName}",
                _poi.Status);

            return Task.FromResult(_poi);
        }

        public Task<PoiDto> DeletePoiImageAsync(int poiId, CancellationToken cancellationToken = default)
        {
            _poi = BuildPoi(
                _poi.Name,
                _poi.Slug,
                _poi.Lat,
                _poi.Lng,
                _poi.Priority,
                _poi.NarrationMode,
                _poi.Description,
                _poi.TtsScript,
                _poi.MapLink,
                null,
                _poi.Status);

            return Task.FromResult(_poi);
        }

        public Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default)
        {
            DeleteRequests.Add(poiId);
            return Task.CompletedTask;
        }

        public readonly record struct ImageUploadCall(int PoiId, string FileName, string ContentType);

        private static PoiDto BuildPoi(
            string name = "Bún mắm Vĩnh Khánh",
            string slug = "bun-mam-vinh-khanh",
            double lat = 10.758,
            double lng = 106.701,
            int priority = 10,
            NarrationMode narrationMode = NarrationMode.Both,
            string description = "Món bún mắm đậm vị.",
            string ttsScript = "Kịch bản nguồn.",
            string? mapLink = "https://maps.test/original",
            string? imageUrl = null,
            PoiStatus status = PoiStatus.Rejected)
        {
            return new PoiDto
            {
                Id = 1,
                Name = name,
                Slug = slug,
                OwnerId = Guid.NewGuid(),
                Lat = lat,
                Lng = lng,
                Priority = priority,
                CategoryId = 2,
                CategoryName = "Hải sản",
                NarrationMode = narrationMode,
                Description = description,
                TtsScript = ttsScript,
                MapLink = mapLink,
                ImageUrl = imageUrl,
                Status = status,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                Translations =
                [
                    new TranslationDto { Id = 1, PoiId = 1, LanguageCode = "vi", Title = "Bún mắm" },
                    new TranslationDto { Id = 2, PoiId = 1, LanguageCode = "en", Title = "Noodle soup" }
                ],
                Geofences =
                [
                    new GeofenceDto
                    {
                        Id = 1,
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
            };
        }
    }

    private sealed class TestAudioPortalService : IAudioPortalService
    {
        private readonly List<AudioDto> _audioItems =
        [
            new AudioDto
            {
                Id = 10,
                PoiId = 1,
                LanguageCode = "vi",
                SourceType = AudioSourceType.Recorded,
                Provider = "owner",
                StoragePath = "audio/vi.mp3",
                Url = "https://cdn.test/audio/vi.mp3",
                Status = AudioStatus.Ready,
                DurationSeconds = 92,
                GeneratedAtUtc = DateTime.UtcNow.AddHours(-2)
            },
            new AudioDto
            {
                Id = 11,
                PoiId = 1,
                LanguageCode = "en",
                SourceType = AudioSourceType.Tts,
                Provider = "tts",
                StoragePath = "audio/en.mp3",
                Url = "https://cdn.test/audio/en.mp3",
                Status = AudioStatus.Generating,
                DurationSeconds = 0,
                GeneratedAtUtc = DateTime.UtcNow.AddHours(-1)
            }
        ];

        public List<UploadAudioRequest> UploadRequests { get; } = [];

        public Task<IReadOnlyList<AudioDto>> GetByPoiAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AudioDto>>(
                _audioItems.Where(item => item.PoiId == poiId).ToArray());
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
            UploadRequests.Add(request);

            var item = new AudioDto
            {
                Id = 100 + UploadRequests.Count,
                PoiId = request.PoiId,
                LanguageCode = request.LanguageCode,
                SourceType = AudioSourceType.Recorded,
                Provider = "owner-upload",
                StoragePath = $"audio/{request.FileName}",
                Url = $"https://cdn.test/audio/{request.FileName}",
                Status = AudioStatus.Ready,
                DurationSeconds = 75,
                GeneratedAtUtc = DateTime.UtcNow
            };

            _audioItems.Add(item);
            return Task.FromResult(item);
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
        public List<ModerationRequestDto> Items { get; set; } =
        [
            new ModerationRequestDto
            {
                Id = 301,
                EntityType = "poi",
                EntityId = "1",
                Status = ModerationStatus.Rejected,
                RequestedBy = Guid.NewGuid(),
                ReviewNote = "Thiếu mô tả nguồn rõ ràng.",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-4)
            }
        ];

        public List<CreateModerationRequest> CreatedRequests { get; } = [];

        public Task<IReadOnlyList<ModerationRequestDto>> GetMineAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ModerationRequestDto>>(Items.ToArray());
        }

        public Task<ModerationRequestDto> CreateAsync(CreateModerationRequest request, CancellationToken cancellationToken = default)
        {
            CreatedRequests.Add(request);

            var item = new ModerationRequestDto
            {
                Id = 400 + CreatedRequests.Count,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                Status = ModerationStatus.Pending,
                RequestedBy = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow
            };

            Items.Add(item);
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
                Id = 1,
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
                new CategoryDto { Id = 1, Name = "Hải sản", Slug = "hai-san", Description = "Nhóm hải sản", Icon = "seafood", DisplayOrder = 1, IsActive = true },
                new CategoryDto { Id = 2, Name = "Bún/Phở", Slug = "bun-pho", Description = "Nhóm bún phở", Icon = "noodle", DisplayOrder = 2, IsActive = true },
                new CategoryDto { Id = 3, Name = "Ăn vặt", Slug = "an-vat", Description = "Nhóm ăn vặt", Icon = "snack", DisplayOrder = 3, IsActive = true }
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
