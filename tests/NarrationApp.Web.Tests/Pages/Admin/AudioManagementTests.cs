using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Admin;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Admin;

public sealed class AudioManagementTests : TestContext
{
    [Fact]
    public void Audio_management_dashboard_uses_control_room_layout_and_generate_modal()
    {
        var audioService = new TestAudioPortalService();
        var adminService = new TestAdminPortalService();
        var translationService = new TestTranslationPortalService();
        var languageService = new TestLanguagePortalService();
        var refreshPump = new TestAudioRefreshPump();

        Services.AddSingleton<IAudioPortalService>(audioService);
        Services.AddSingleton<IAdminPortalService>(adminService);
        Services.AddSingleton<ITranslationPortalService>(translationService);
        Services.AddSingleton<ILanguagePortalService>(languageService);
        Services.AddSingleton<IAudioRefreshPump>(refreshPump);

        var cut = RenderComponent<AudioManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Audio Management", cut.Markup);
            Assert.Contains("Batch Generate", cut.Markup);
            Assert.Contains("Audio sẵn sàng", cut.Markup);
            Assert.Contains("Tổng audio assets", cut.Markup);
            Assert.Contains("Danh sách Audio Assets", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.DoesNotContain("Luồng nguồn tiếng Việt", cut.Markup);
            Assert.DoesNotContain("Bàn chất lượng audio", cut.Markup);
            Assert.DoesNotContain("Upload Audio", cut.Find(".audio-table__header").TextContent);
            Assert.DoesNotContain("Tạo TTS", cut.Find(".audio-table__header").TextContent);
            Assert.DoesNotContain("Refresh", cut.Find(".audio-table__header").TextContent);
            Assert.Empty(cut.FindAll(".audio-kpi-card__icon"));
        });

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Generate", cut.Find("button[data-action='row-open-generate-7']").TextContent.Trim());
            Assert.Equal("Upload", cut.Find("button[data-action='row-open-upload-7']").TextContent.Trim());
        });

        cut.Find("button[data-action='row-open-generate-7']").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Generate Audio Đa Ngôn Ngữ", cut.Markup);
            Assert.Contains("Google Cloud TTS", cut.Markup);
            Assert.Contains("Standard (Tiêu chuẩn — tiết kiệm)", cut.Markup);
            Assert.Contains("WaveNet (Tự nhiên — cao cấp)", cut.Markup);
            Assert.Contains("Neural2 (Tiên tiến nhất)", cut.Markup);
        });

        cut.Find("input[data-field='language-ko']").Change(true);
        cut.Find("select[data-field='voice-profile']").Change("neural2");
        cut.Find("button[data-action='generate-selected-audio']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã tạo 3 audio cho Bún mắm Vĩnh Khánh.", cut.Markup);
            Assert.DoesNotContain("Generate Audio Đa Ngôn Ngữ", cut.Markup);
        });

        Assert.Single(audioService.GeneratedRequests);
        Assert.Equal("vi", audioService.GeneratedRequests[0].LanguageCode);
        Assert.All(audioService.GeneratedRequests, request => Assert.Equal("neural2", request.VoiceProfile));
        Assert.Equal(2, audioService.GeneratedFromTranslationRequests.Count);
        Assert.Equal("en", audioService.GeneratedFromTranslationRequests[0].LanguageCode);
        Assert.Equal("ja", audioService.GeneratedFromTranslationRequests[1].LanguageCode);
        Assert.Empty(translationService.AutoRequests);
    }

    [Fact]
    public async Task Audio_management_auto_refreshes_processing_rows_without_manual_refresh()
    {
        var audioService = new SequencedAudioPortalService();
        var adminService = new TestAdminPortalService();
        var translationService = new TestTranslationPortalService();
        var languageService = new TestLanguagePortalService();
        var refreshPump = new TestAudioRefreshPump();

        Services.AddSingleton<IAudioPortalService>(audioService);
        Services.AddSingleton<IAdminPortalService>(adminService);
        Services.AddSingleton<ITranslationPortalService>(translationService);
        Services.AddSingleton<ILanguagePortalService>(languageService);
        Services.AddSingleton<IAudioRefreshPump>(refreshPump);

        var cut = RenderComponent<AudioManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".audio-language-chip.is-generating"));
            Assert.Contains("Đang xử lý", cut.Markup);
        });

        await refreshPump.TriggerAsync();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".audio-language-chip.is-generating"));
            Assert.True(cut.FindAll(".audio-language-chip.is-ready").Count >= 1);
            Assert.True(audioService.GetCallCount >= 2);
        });
    }

    [Fact]
    public void Audio_management_retries_failed_languages_from_row_action()
    {
        var audioService = new RetryableFailedAudioPortalService();
        var adminService = new TestAdminPortalService();
        var translationService = new TestTranslationPortalService();
        var languageService = new TestLanguagePortalService();
        var refreshPump = new TestAudioRefreshPump();

        Services.AddSingleton<IAudioPortalService>(audioService);
        Services.AddSingleton<IAdminPortalService>(adminService);
        Services.AddSingleton<ITranslationPortalService>(translationService);
        Services.AddSingleton<ILanguagePortalService>(languageService);
        Services.AddSingleton<IAudioRefreshPump>(refreshPump);

        var cut = RenderComponent<AudioManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("failed", cut.Find("tr[data-row-poi='7'] td:nth-child(3) .audio-tag").TextContent.Trim());
            Assert.Equal("Retry lỗi", cut.Find("button[data-action='row-retry-failed-7']").TextContent.Trim());
            Assert.Equal(2, cut.FindAll(".audio-language-chip.is-failed").Count);
        });

        cut.Find("button[data-action='row-retry-failed-7']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã retry 2 audio lỗi cho Bún mắm Vĩnh Khánh.", cut.Markup);
            Assert.Equal("ready", cut.Find("tr[data-row-poi='7'] td:nth-child(3) .audio-tag").TextContent.Trim());
            Assert.Empty(cut.FindAll(".audio-language-chip.is-failed"));
        });

        Assert.Equal(2, audioService.GeneratedFromTranslationRequests.Count);
        Assert.Equal(["en", "ja"], audioService.GeneratedFromTranslationRequests.Select(item => item.LanguageCode).OrderBy(code => code).ToArray());
    }

    [Fact]
    public void Audio_management_retries_all_failed_audio_from_filtered_backlog()
    {
        var audioService = new MultiPoiRetryableAudioPortalService();
        var adminService = new MultiPoiAdminPortalService();
        var translationService = new MultiPoiTranslationPortalService();
        var languageService = new TestLanguagePortalService();
        var refreshPump = new TestAudioRefreshPump();

        Services.AddSingleton<IAudioPortalService>(audioService);
        Services.AddSingleton<IAdminPortalService>(adminService);
        Services.AddSingleton<ITranslationPortalService>(translationService);
        Services.AddSingleton<ILanguagePortalService>(languageService);
        Services.AddSingleton<IAudioRefreshPump>(refreshPump);

        var cut = RenderComponent<AudioManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Retry tất cả lỗi", cut.Find("button[data-action='retry-all-failed-audio']").TextContent.Trim());
            Assert.Equal(2, cut.FindAll("button[data-action^='row-retry-failed-']").Count);
        });

        cut.Find("select[data-field='category-filter']").Change("Bún/Phở");

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("tr[data-row-poi]"));
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
        });

        cut.Find("button[data-action='retry-all-failed-audio']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã retry 2 audio lỗi trên 1 POI.", cut.Markup);
            Assert.Equal("ready", cut.Find("tr[data-row-poi='7'] td:nth-child(3) .audio-tag").TextContent.Trim());
            Assert.Equal("0", cut.Find("button[data-action='retry-all-failed-audio']").GetAttribute("data-failed-pois"));
        });

        Assert.Equal(2, audioService.GeneratedFromTranslationRequests.Count);
        Assert.DoesNotContain(audioService.GeneratedFromTranslationRequests, item => item.PoiId == 9);
    }

    private sealed class TestLanguagePortalService : ILanguagePortalService
    {
        public Task<IReadOnlyList<ManagedLanguageDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ManagedLanguageDto>>(
            [
                new ManagedLanguageDto { Code = "vi", DisplayName = "Tiếng Việt", NativeName = "Tiếng Việt", FlagCode = "VN", Role = ManagedLanguageRole.Source, IsActive = true, TranslationCoverageCount = 1, TranslationCoverageTotal = 1, AudioCount = 1 },
                new ManagedLanguageDto { Code = "en", DisplayName = "English", NativeName = "English", FlagCode = "GB", Role = ManagedLanguageRole.TranslationAudio, IsActive = true, TranslationCoverageCount = 1, TranslationCoverageTotal = 1, AudioCount = 0 },
                new ManagedLanguageDto { Code = "ja", DisplayName = "Japanese", NativeName = "日本語", FlagCode = "JP", Role = ManagedLanguageRole.TranslationAudio, IsActive = true, TranslationCoverageCount = 1, TranslationCoverageTotal = 1, AudioCount = 0 },
                new ManagedLanguageDto { Code = "ko", DisplayName = "Korean", NativeName = "한국어", FlagCode = "KR", Role = ManagedLanguageRole.TranslationAudio, IsActive = true, TranslationCoverageCount = 0, TranslationCoverageTotal = 1, AudioCount = 0 }
            ]);
        }

        public Task<ManagedLanguageDto> CreateAsync(CreateManagedLanguageRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(string code, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestAdminPortalService : IAdminPortalService
    {
        public Task<DashboardDto> GetOverviewAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<AdminPoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AdminPoiDto>>(
            [
                new AdminPoiDto
                {
                    Id = 7,
                    Name = "Bún mắm Vĩnh Khánh",
                    Slug = "bun-mam-vinh-khanh",
                    OwnerName = "Owner Một",
                    OwnerEmail = "owner@narration.app",
                    CategoryName = "Bún/Phở",
                    Description = "Quầy bún mắm đậm vị về đêm.",
                    TtsScript = "Bún mắm Vĩnh Khánh là điểm dừng nổi bật của tuyến ẩm thực đêm.",
                    Status = PoiStatus.PendingReview
                }
            ]);
        }

        public Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ModerationRequestDto>> GetPendingModerationAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ModerationRequestDto> ApproveModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ModerationRequestDto> RejectModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<TopPoiDto>> GetTopPoisAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AudioPlayAnalyticsDto> GetAudioPlayAnalyticsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestAudioPortalService : IAudioPortalService
    {
        private readonly List<AudioDto> _items =
        [
            new AudioDto
            {
                Id = 88,
                PoiId = 7,
                LanguageCode = "vi",
                SourceType = AudioSourceType.Recorded,
                Provider = "manual-upload-vi",
                StoragePath = "audio/vinh-khanh.mp3",
                Url = "https://localhost:5001/api/audio/88/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 30,
                GeneratedAtUtc = DateTime.UtcNow
            }
        ];

        public List<TtsGenerateRequest> GeneratedRequests { get; } = [];

        public List<GenerateAudioFromTranslationRequest> GeneratedFromTranslationRequests { get; } = [];

        public Task<IReadOnlyList<AudioDto>> GetByPoiAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AudioDto>>(_items.Where(item => item.PoiId == poiId).ToArray());
        }

        public Task<AudioDto> GenerateTtsAsync(TtsGenerateRequest request, CancellationToken cancellationToken = default)
        {
            GeneratedRequests.Add(request);

            var item = new AudioDto
            {
                Id = 100 + GeneratedRequests.Count,
                PoiId = request.PoiId,
                LanguageCode = request.LanguageCode,
                SourceType = AudioSourceType.Tts,
                Provider = $"mock-google-tts-{request.LanguageCode}",
                StoragePath = $"audio/{request.LanguageCode}.mp3",
                Url = $"https://localhost:5001/api/audio/{100 + GeneratedRequests.Count}/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 42,
                GeneratedAtUtc = DateTime.UtcNow
            };

            _items.Add(item);
            return Task.FromResult(item);
        }

        public Task<AudioDto> GenerateFromTranslationAsync(GenerateAudioFromTranslationRequest request, CancellationToken cancellationToken = default)
        {
            GeneratedFromTranslationRequests.Add(request);

            var item = new AudioDto
            {
                Id = 300 + GeneratedFromTranslationRequests.Count,
                PoiId = request.PoiId,
                LanguageCode = request.LanguageCode,
                SourceType = AudioSourceType.Tts,
                Provider = $"mock-translation-audio-{request.LanguageCode}",
                StoragePath = $"audio/{request.LanguageCode}-translation.mp3",
                Url = $"https://localhost:5001/api/audio/{300 + GeneratedFromTranslationRequests.Count}/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 55,
                GeneratedAtUtc = DateTime.UtcNow
            };

            _items.Add(item);
            return Task.FromResult(item);
        }

        public Task<AudioDto> UploadAsync(UploadAudioRequest request, Stream content, CancellationToken cancellationToken = default)
        {
            var item = new AudioDto
            {
                Id = 200,
                PoiId = request.PoiId,
                LanguageCode = request.LanguageCode,
                SourceType = AudioSourceType.Recorded,
                Provider = "manual-upload-vi",
                StoragePath = "audio/uploaded.mp3",
                Url = "https://localhost:5001/api/audio/200/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 75,
                GeneratedAtUtc = DateTime.UtcNow
            };

            _items.Add(item);
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

    private sealed class TestTranslationPortalService : ITranslationPortalService
    {
        public List<string> AutoRequests { get; } = [];

        public Task<IReadOnlyList<TranslationDto>> GetByPoiAsync(int poiId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TranslationDto>>(
            [
                new TranslationDto
                {
                    Id = 1,
                    PoiId = poiId,
                    LanguageCode = "vi",
                    Title = "Bún mắm Vĩnh Khánh",
                    Description = "Mô tả VI",
                    Story = "Script tiếng Việt",
                    Highlight = "Ẩm thực đêm",
                    IsFallback = false,
                    WorkflowStatus = TranslationWorkflowStatus.Source
                },
                new TranslationDto
                {
                    Id = 2,
                    PoiId = poiId,
                    LanguageCode = "en",
                    Title = "Vinh Khanh food street",
                    Description = "English description",
                    Story = "English story for audio",
                    Highlight = "Night food scene",
                    IsFallback = false,
                    WorkflowStatus = TranslationWorkflowStatus.Reviewed
                },
                new TranslationDto
                {
                    Id = 3,
                    PoiId = poiId,
                    LanguageCode = "ja",
                    Title = "ヴィンカーン通り",
                    Description = "Japanese description",
                    Story = "Japanese story for audio",
                    Highlight = "屋台街",
                    IsFallback = false,
                    WorkflowStatus = TranslationWorkflowStatus.Reviewed
                }
            ]);
        }

        public Task<TranslationDto> SaveAsync(CreateTranslationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<TranslationDto> AutoTranslateAsync(int poiId, string targetLanguage, CancellationToken cancellationToken = default)
        {
            AutoRequests.Add(targetLanguage);
            return Task.FromResult(new TranslationDto
            {
                Id = 501,
                PoiId = poiId,
                LanguageCode = targetLanguage,
                Title = "Vinh Khanh seafood stop",
                Description = "Translated description",
                Story = "Translated story for audio",
                Highlight = "Food street",
                IsFallback = false
            });
        }

        public Task DeleteAsync(int translationId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestAudioRefreshPump : IAudioRefreshPump
    {
        private Func<CancellationToken, Task>? _onTick;

        public IAsyncDisposable Start(Func<CancellationToken, Task> onTick, TimeSpan interval)
        {
            _onTick = onTick;
            return new NoopAsyncDisposable();
        }

        public Task TriggerAsync(CancellationToken cancellationToken = default)
        {
            return _onTick?.Invoke(cancellationToken) ?? Task.CompletedTask;
        }

        private sealed class NoopAsyncDisposable : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class SequencedAudioPortalService : IAudioPortalService
    {
        private readonly IReadOnlyList<AudioDto> _processingItems =
        [
            new AudioDto
            {
                Id = 901,
                PoiId = 7,
                LanguageCode = "en",
                SourceType = AudioSourceType.Tts,
                Provider = "background-google-tts",
                StoragePath = "audio/en-processing.mp3",
                Url = "https://localhost:5001/api/audio/901/stream",
                Status = AudioStatus.Generating,
                DurationSeconds = 0,
                GeneratedAtUtc = DateTime.UtcNow
            }
        ];

        private readonly IReadOnlyList<AudioDto> _readyItems =
        [
            new AudioDto
            {
                Id = 901,
                PoiId = 7,
                LanguageCode = "en",
                SourceType = AudioSourceType.Tts,
                Provider = "background-google-tts",
                StoragePath = "audio/en-ready.mp3",
                Url = "https://localhost:5001/api/audio/901/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 47,
                GeneratedAtUtc = DateTime.UtcNow
            }
        ];

        public int GetCallCount { get; private set; }

        public Task<IReadOnlyList<AudioDto>> GetByPoiAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            IReadOnlyList<AudioDto> items = GetCallCount == 1 ? _processingItems : _readyItems;
            return Task.FromResult(items);
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

    private sealed class RetryableFailedAudioPortalService : IAudioPortalService
    {
        private readonly List<AudioDto> _items =
        [
            new AudioDto
            {
                Id = 880,
                PoiId = 7,
                LanguageCode = "vi",
                SourceType = AudioSourceType.Recorded,
                Provider = "manual-upload-vi",
                StoragePath = "audio/vi.mp3",
                Url = "https://localhost:5001/api/audio/880/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 30,
                GeneratedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            },
            new AudioDto
            {
                Id = 881,
                PoiId = 7,
                LanguageCode = "en",
                SourceType = AudioSourceType.Tts,
                Provider = "background-google-tts",
                StoragePath = "audio/en-failed.mp3",
                Url = "https://localhost:5001/api/audio/881/stream",
                Status = AudioStatus.Failed,
                DurationSeconds = 0,
                GeneratedAtUtc = DateTime.UtcNow.AddMinutes(-4)
            },
            new AudioDto
            {
                Id = 882,
                PoiId = 7,
                LanguageCode = "ja",
                SourceType = AudioSourceType.Tts,
                Provider = "background-google-tts",
                StoragePath = "audio/ja-failed.mp3",
                Url = "https://localhost:5001/api/audio/882/stream",
                Status = AudioStatus.Failed,
                DurationSeconds = 0,
                GeneratedAtUtc = DateTime.UtcNow.AddMinutes(-3)
            }
        ];

        public List<GenerateAudioFromTranslationRequest> GeneratedFromTranslationRequests { get; } = [];

        public Task<IReadOnlyList<AudioDto>> GetByPoiAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AudioDto>>(_items.Where(item => item.PoiId == poiId).ToArray());
        }

        public Task<AudioDto> GenerateTtsAsync(TtsGenerateRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AudioDto> GenerateFromTranslationAsync(GenerateAudioFromTranslationRequest request, CancellationToken cancellationToken = default)
        {
            GeneratedFromTranslationRequests.Add(request);

            var generated = new AudioDto
            {
                Id = 990 + GeneratedFromTranslationRequests.Count,
                PoiId = request.PoiId,
                LanguageCode = request.LanguageCode,
                SourceType = AudioSourceType.Tts,
                Provider = $"retry-google-tts-{request.LanguageCode}",
                StoragePath = $"audio/{request.LanguageCode}-retry.mp3",
                Url = $"https://localhost:5001/api/audio/{990 + GeneratedFromTranslationRequests.Count}/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 44,
                GeneratedAtUtc = DateTime.UtcNow
            };

            _items.Add(generated);
            return Task.FromResult(generated);
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

    private sealed class MultiPoiRetryableAudioPortalService : IAudioPortalService
    {
        private readonly List<AudioDto> _items =
        [
            new AudioDto
            {
                Id = 880,
                PoiId = 7,
                LanguageCode = "vi",
                SourceType = AudioSourceType.Recorded,
                Provider = "manual-upload-vi",
                StoragePath = "audio/vi-7.mp3",
                Url = "https://localhost:5001/api/audio/880/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 30,
                GeneratedAtUtc = DateTime.UtcNow.AddMinutes(-8)
            },
            new AudioDto
            {
                Id = 881,
                PoiId = 7,
                LanguageCode = "en",
                SourceType = AudioSourceType.Tts,
                Provider = "background-google-tts",
                StoragePath = "audio/en-7-failed.mp3",
                Url = "https://localhost:5001/api/audio/881/stream",
                Status = AudioStatus.Failed,
                DurationSeconds = 0,
                GeneratedAtUtc = DateTime.UtcNow.AddMinutes(-4)
            },
            new AudioDto
            {
                Id = 882,
                PoiId = 7,
                LanguageCode = "ja",
                SourceType = AudioSourceType.Tts,
                Provider = "background-google-tts",
                StoragePath = "audio/ja-7-failed.mp3",
                Url = "https://localhost:5001/api/audio/882/stream",
                Status = AudioStatus.Failed,
                DurationSeconds = 0,
                GeneratedAtUtc = DateTime.UtcNow.AddMinutes(-3)
            },
            new AudioDto
            {
                Id = 980,
                PoiId = 9,
                LanguageCode = "vi",
                SourceType = AudioSourceType.Recorded,
                Provider = "manual-upload-vi",
                StoragePath = "audio/vi-9.mp3",
                Url = "https://localhost:5001/api/audio/980/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 33,
                GeneratedAtUtc = DateTime.UtcNow.AddMinutes(-7)
            },
            new AudioDto
            {
                Id = 981,
                PoiId = 9,
                LanguageCode = "en",
                SourceType = AudioSourceType.Tts,
                Provider = "background-google-tts",
                StoragePath = "audio/en-9-failed.mp3",
                Url = "https://localhost:5001/api/audio/981/stream",
                Status = AudioStatus.Failed,
                DurationSeconds = 0,
                GeneratedAtUtc = DateTime.UtcNow.AddMinutes(-2)
            }
        ];

        public List<GenerateAudioFromTranslationRequest> GeneratedFromTranslationRequests { get; } = [];

        public Task<IReadOnlyList<AudioDto>> GetByPoiAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AudioDto>>(_items.Where(item => item.PoiId == poiId).ToArray());
        }

        public Task<AudioDto> GenerateTtsAsync(TtsGenerateRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AudioDto> GenerateFromTranslationAsync(GenerateAudioFromTranslationRequest request, CancellationToken cancellationToken = default)
        {
            GeneratedFromTranslationRequests.Add(request);

            var generated = new AudioDto
            {
                Id = 1990 + GeneratedFromTranslationRequests.Count,
                PoiId = request.PoiId,
                LanguageCode = request.LanguageCode,
                SourceType = AudioSourceType.Tts,
                Provider = $"retry-google-tts-{request.LanguageCode}",
                StoragePath = $"audio/{request.PoiId}-{request.LanguageCode}-retry.mp3",
                Url = $"https://localhost:5001/api/audio/{1990 + GeneratedFromTranslationRequests.Count}/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 50,
                GeneratedAtUtc = DateTime.UtcNow
            };

            _items.Add(generated);
            return Task.FromResult(generated);
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

    private sealed class MultiPoiAdminPortalService : IAdminPortalService
    {
        public Task<DashboardDto> GetOverviewAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<AdminPoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AdminPoiDto>>(
            [
                new AdminPoiDto
                {
                    Id = 7,
                    Name = "Bún mắm Vĩnh Khánh",
                    Slug = "bun-mam-vinh-khanh",
                    OwnerName = "Owner Một",
                    OwnerEmail = "owner@narration.app",
                    CategoryName = "Bún/Phở",
                    Description = "Quầy bún mắm đậm vị về đêm.",
                    TtsScript = "Bún mắm Vĩnh Khánh là điểm dừng nổi bật của tuyến ẩm thực đêm.",
                    Status = PoiStatus.PendingReview
                },
                new AdminPoiDto
                {
                    Id = 9,
                    Name = "Cà phê Xóm Chiếu",
                    Slug = "ca-phe-xom-chieu",
                    OwnerName = "Owner Hai",
                    OwnerEmail = "owner2@narration.app",
                    CategoryName = "Đồ uống",
                    Description = "Quán cà phê đầu hẻm.",
                    TtsScript = "Cà phê Xóm Chiếu là điểm dừng thư giãn đầu tuyến.",
                    Status = PoiStatus.Published
                }
            ]);
        }

        public Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ModerationRequestDto>> GetPendingModerationAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ModerationRequestDto> ApproveModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ModerationRequestDto> RejectModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<TopPoiDto>> GetTopPoisAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AudioPlayAnalyticsDto> GetAudioPlayAnalyticsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class MultiPoiTranslationPortalService : ITranslationPortalService
    {
        public Task<IReadOnlyList<TranslationDto>> GetByPoiAsync(int poiId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TranslationDto> items = poiId switch
            {
                7 =>
                [
                    new TranslationDto
                    {
                        Id = 1,
                        PoiId = poiId,
                        LanguageCode = "vi",
                        Title = "Bún mắm Vĩnh Khánh",
                        Description = "Mô tả VI",
                        Story = "Script tiếng Việt",
                        Highlight = "Ẩm thực đêm",
                        IsFallback = false,
                        WorkflowStatus = TranslationWorkflowStatus.Source
                    },
                    new TranslationDto
                    {
                        Id = 2,
                        PoiId = poiId,
                        LanguageCode = "en",
                        Title = "Vinh Khanh food street",
                        Description = "English description",
                        Story = "English story for audio",
                        Highlight = "Night food scene",
                        IsFallback = false,
                        WorkflowStatus = TranslationWorkflowStatus.Reviewed
                    },
                    new TranslationDto
                    {
                        Id = 3,
                        PoiId = poiId,
                        LanguageCode = "ja",
                        Title = "ヴィンカーン通り",
                        Description = "Japanese description",
                        Story = "Japanese story for audio",
                        Highlight = "屋台街",
                        IsFallback = false,
                        WorkflowStatus = TranslationWorkflowStatus.Reviewed
                    }
                ],
                9 =>
                [
                    new TranslationDto
                    {
                        Id = 4,
                        PoiId = poiId,
                        LanguageCode = "vi",
                        Title = "Cà phê Xóm Chiếu",
                        Description = "Mô tả VI",
                        Story = "Script tiếng Việt",
                        Highlight = "Cà phê hẻm",
                        IsFallback = false,
                        WorkflowStatus = TranslationWorkflowStatus.Source
                    },
                    new TranslationDto
                    {
                        Id = 5,
                        PoiId = poiId,
                        LanguageCode = "en",
                        Title = "Xom Chieu coffee",
                        Description = "English description",
                        Story = "English story for audio",
                        Highlight = "Coffee stop",
                        IsFallback = false,
                        WorkflowStatus = TranslationWorkflowStatus.Reviewed
                    }
                ],
                _ => []
            };

            return Task.FromResult(items);
        }

        public Task<TranslationDto> SaveAsync(CreateTranslationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<TranslationDto> AutoTranslateAsync(int poiId, string targetLanguage, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(int translationId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
