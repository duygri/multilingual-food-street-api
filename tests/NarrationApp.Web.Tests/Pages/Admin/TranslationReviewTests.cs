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

public sealed class TranslationReviewTests : TestContext
{
    [Fact]
    public void Auto_translate_save_and_delete_translation_from_admin_surface()
    {
        var adminService = new TestAdminPortalService();
        var translationService = new TestTranslationPortalService();
        var languageService = new TestLanguagePortalService();
        var audioService = new TestAudioPortalService();

        Services.AddSingleton<IAdminPortalService>(adminService);
        Services.AddSingleton<ITranslationPortalService>(translationService);
        Services.AddSingleton<ILanguagePortalService>(languageService);
        Services.AddSingleton<IAudioPortalService>(audioService);

        var cut = RenderComponent<TranslationReview>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.Contains("Tổng bản dịch", cut.Markup);
            Assert.Contains("POI có ≥2 ngôn ngữ", cut.Markup);
            Assert.Contains("Auto-translated", cut.Markup);
            Assert.Contains("Đã biên tập thủ công", cut.Markup);
            Assert.Contains("Quản lý bản dịch", cut.Markup);
            Assert.Contains("Auto-translate All", cut.Markup);
            Assert.Contains("VI VN", cut.Markup);
            Assert.Contains("EN GB", cut.Markup);
            Assert.Contains("JA JP", cut.Markup);
            Assert.DoesNotContain("Published POIs", cut.Markup);
            Assert.DoesNotContain("Translation review", cut.Markup);
            Assert.DoesNotContain("Quick actions", cut.Markup);
            Assert.Equal(4, cut.FindAll(".translation-review__kpi-card").Count);
            Assert.True(cut.FindAll(".translation-review__matrix-badge").Count >= 3);
            Assert.Empty(cut.FindAll(".translation-review__kpi-icon"));
            Assert.Empty(cut.FindAll("[data-panel='translation-review']"));
            Assert.Contains("data-audio-state=\"3-vi-ready\"", cut.Markup);
            Assert.Contains("data-audio-state=\"3-en-missing\"", cut.Markup);
            Assert.Contains("data-audio-state=\"3-ja-missing\"", cut.Markup);
        });

        cut.Find("button[data-action='auto-translate-all']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã tự động bổ sung 2 bản dịch.", cut.Markup);
            Assert.Contains("data-audio-state=\"3-en-generating\"", cut.Markup);
            Assert.Contains("data-audio-state=\"3-ja-generating\"", cut.Markup);
        });

        cut.Find("button[data-action='toggle-review-panel']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-panel='translation-review']")));

        cut.Find("select[data-field='translation-poi']").Change("3");
        cut.Find("select[data-field='translation-language']").Change("en");
        cut.Find("input[data-field='translation-title']").Change("Vinh Khanh review copy");
        cut.Find("button[data-action='save-translation']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã lưu bản dịch", cut.Markup);
            Assert.Contains("Vinh Khanh review copy", cut.Markup);
        });

        cut.Find("button[data-action='delete-translation']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Vinh Khanh review copy", cut.Markup);
            Assert.Contains("Đã xóa bản dịch", cut.Markup);
        });

        Assert.Equal(2, translationService.AutoRequests.Count);
        Assert.Single(translationService.SaveRequests);
        Assert.Single(translationService.DeleteRequests);
        Assert.Equal("en", translationService.SaveRequests[0].LanguageCode);
    }

    [Fact]
    public void Translation_review_panel_retries_failed_audio_for_selected_language()
    {
        var adminService = new TestAdminPortalService();
        var translationService = new RetryableTranslationPortalService();
        var languageService = new TestLanguagePortalService();
        var audioService = new RetryableTranslationAudioPortalService();

        Services.AddSingleton<IAdminPortalService>(adminService);
        Services.AddSingleton<ITranslationPortalService>(translationService);
        Services.AddSingleton<ILanguagePortalService>(languageService);
        Services.AddSingleton<IAudioPortalService>(audioService);

        var cut = RenderComponent<TranslationReview>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("data-audio-state=\"3-en-failed\"", cut.Markup);
        });

        cut.Find("button[data-action='toggle-review-panel']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-panel='translation-review']")));

        cut.Find("select[data-field='translation-language']").Change("en");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("failed", cut.Find("[data-selected-audio-state]").GetAttribute("data-selected-audio-state"));
            Assert.Equal("Retry audio", cut.Find("button[data-action='retry-audio']").TextContent.Trim());
        });

        cut.Find("button[data-action='retry-audio']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Đã retry audio en cho Bún mắm Vĩnh Khánh.", cut.Markup);
            Assert.Contains("data-audio-state=\"3-en-ready\"", cut.Markup);
            Assert.Equal("ready", cut.Find("[data-selected-audio-state]").GetAttribute("data-selected-audio-state"));
        });

        Assert.Single(audioService.GeneratedFromTranslationRequests);
        Assert.Equal("en", audioService.GeneratedFromTranslationRequests[0].LanguageCode);
    }

    private sealed class TestAudioPortalService : IAudioPortalService
    {
        private int _getCallCount;

        public Task<IReadOnlyList<AudioDto>> GetByPoiAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default)
        {
            _getCallCount++;

            IReadOnlyList<AudioDto> items = _getCallCount == 1
                ? 
                [
                    new AudioDto
                    {
                        Id = 601,
                        PoiId = poiId,
                        LanguageCode = "vi",
                        SourceType = AudioSourceType.Recorded,
                        Provider = "manual-upload",
                        StoragePath = "audio/vi.mp3",
                        Url = "https://localhost:5001/api/audio/601/stream",
                        Status = AudioStatus.Ready,
                        DurationSeconds = 40,
                        GeneratedAtUtc = DateTime.UtcNow
                    }
                ]
                :
                [
                    new AudioDto
                    {
                        Id = 601,
                        PoiId = poiId,
                        LanguageCode = "vi",
                        SourceType = AudioSourceType.Recorded,
                        Provider = "manual-upload",
                        StoragePath = "audio/vi.mp3",
                        Url = "https://localhost:5001/api/audio/601/stream",
                        Status = AudioStatus.Ready,
                        DurationSeconds = 40,
                        GeneratedAtUtc = DateTime.UtcNow
                    },
                    new AudioDto
                    {
                        Id = 602,
                        PoiId = poiId,
                        LanguageCode = "en",
                        SourceType = AudioSourceType.Tts,
                        Provider = "background-google-tts",
                        StoragePath = "audio/en.mp3",
                        Url = "https://localhost:5001/api/audio/602/stream",
                        Status = AudioStatus.Generating,
                        DurationSeconds = 0,
                        GeneratedAtUtc = DateTime.UtcNow
                    },
                    new AudioDto
                    {
                        Id = 603,
                        PoiId = poiId,
                        LanguageCode = "ja",
                        SourceType = AudioSourceType.Tts,
                        Provider = "background-google-tts",
                        StoragePath = "audio/ja.mp3",
                        Url = "https://localhost:5001/api/audio/603/stream",
                        Status = AudioStatus.Requested,
                        DurationSeconds = 0,
                        GeneratedAtUtc = DateTime.UtcNow
                    }
                ];

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

    private sealed class RetryableTranslationAudioPortalService : IAudioPortalService
    {
        private readonly List<AudioDto> _items =
        [
            new AudioDto
            {
                Id = 601,
                PoiId = 3,
                LanguageCode = "vi",
                SourceType = AudioSourceType.Recorded,
                Provider = "manual-upload",
                StoragePath = "audio/vi.mp3",
                Url = "https://localhost:5001/api/audio/601/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 40,
                GeneratedAtUtc = DateTime.UtcNow.AddMinutes(-8)
            },
            new AudioDto
            {
                Id = 602,
                PoiId = 3,
                LanguageCode = "en",
                SourceType = AudioSourceType.Tts,
                Provider = "background-google-tts",
                StoragePath = "audio/en-failed.mp3",
                Url = "https://localhost:5001/api/audio/602/stream",
                Status = AudioStatus.Failed,
                DurationSeconds = 0,
                GeneratedAtUtc = DateTime.UtcNow.AddMinutes(-4)
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
                Id = 700 + GeneratedFromTranslationRequests.Count,
                PoiId = request.PoiId,
                LanguageCode = request.LanguageCode,
                SourceType = AudioSourceType.Tts,
                Provider = $"retry-google-tts-{request.LanguageCode}",
                StoragePath = $"audio/{request.LanguageCode}-retry.mp3",
                Url = $"https://localhost:5001/api/audio/{700 + GeneratedFromTranslationRequests.Count}/stream",
                Status = AudioStatus.Ready,
                DurationSeconds = 48,
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

    private sealed class TestLanguagePortalService : ILanguagePortalService
    {
        public Task<IReadOnlyList<ManagedLanguageDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ManagedLanguageDto>>(
            [
                new ManagedLanguageDto { Code = "vi", DisplayName = "Tiếng Việt", NativeName = "Tiếng Việt", FlagCode = "VN", Role = ManagedLanguageRole.Source, IsActive = true, TranslationCoverageCount = 1, TranslationCoverageTotal = 1, AudioCount = 0 },
                new ManagedLanguageDto { Code = "en", DisplayName = "English", NativeName = "English", FlagCode = "GB", Role = ManagedLanguageRole.TranslationAudio, IsActive = true, TranslationCoverageCount = 0, TranslationCoverageTotal = 1, AudioCount = 0 },
                new ManagedLanguageDto { Code = "ja", DisplayName = "Japanese", NativeName = "日本語", FlagCode = "JP", Role = ManagedLanguageRole.TranslationAudio, IsActive = true, TranslationCoverageCount = 0, TranslationCoverageTotal = 1, AudioCount = 0 }
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
                    Id = 3,
                    Name = "Bún mắm Vĩnh Khánh",
                    Slug = "bun-mam-vinh-khanh",
                    OwnerName = "Owner Một",
                    OwnerEmail = "owner@narration.app",
                    CategoryName = "Bún/Phở",
                    Description = "Trục món đêm nổi bật của Quận 4.",
                    TtsScript = "Một dải phố ăn khuya nổi tiếng.",
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

    private sealed class TestTranslationPortalService : ITranslationPortalService
    {
        private readonly List<TranslationDto> _translations =
        [
            new TranslationDto
            {
                Id = 41,
                PoiId = 3,
                LanguageCode = "vi",
                Title = "Phố Ẩm Thực Vĩnh Khánh",
                Description = "Trục món đêm nổi bật của Quận 4.",
                Story = "Một dải phố ăn khuya nổi tiếng.",
                Highlight = "Ẩm thực đêm",
                IsFallback = false,
                WorkflowStatus = TranslationWorkflowStatus.Source
            }
        ];

        public List<string> AutoRequests { get; } = [];

        public List<CreateTranslationRequest> SaveRequests { get; } = [];

        public List<int> DeleteRequests { get; } = [];

        public Task<IReadOnlyList<TranslationDto>> GetByPoiAsync(int poiId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TranslationDto>>(_translations.Where(item => item.PoiId == poiId).ToArray());
        }

        public Task<TranslationDto> SaveAsync(CreateTranslationRequest request, CancellationToken cancellationToken = default)
        {
            SaveRequests.Add(request);

            var saved = new TranslationDto
            {
                Id = 99,
                PoiId = request.PoiId,
                LanguageCode = request.LanguageCode,
                Title = request.Title,
                Description = request.Description,
                Story = request.Story,
                Highlight = request.Highlight,
                IsFallback = request.IsFallback,
                WorkflowStatus = request.LanguageCode == "vi"
                    ? TranslationWorkflowStatus.Source
                    : TranslationWorkflowStatus.Reviewed
            };

            _translations.RemoveAll(item => item.PoiId == request.PoiId && item.LanguageCode == request.LanguageCode);
            _translations.Add(saved);
            return Task.FromResult(saved);
        }

        public Task<TranslationDto> AutoTranslateAsync(int poiId, string targetLanguage, CancellationToken cancellationToken = default)
        {
            AutoRequests.Add(targetLanguage);

            var generated = new TranslationDto
            {
                Id = 77,
                PoiId = poiId,
                LanguageCode = targetLanguage,
                Title = "Vinh Khanh food street",
                Description = "A strong late-night dining strip.",
                Story = "A famous after-dark corridor for street food.",
                Highlight = "Night food scene",
                IsFallback = true,
                WorkflowStatus = TranslationWorkflowStatus.AutoTranslated
            };

            _translations.RemoveAll(item => item.PoiId == poiId && item.LanguageCode == targetLanguage);
            _translations.Add(generated);
            return Task.FromResult(generated);
        }

        public Task DeleteAsync(int translationId, CancellationToken cancellationToken = default)
        {
            DeleteRequests.Add(translationId);
            _translations.RemoveAll(item => item.Id == translationId);
            return Task.CompletedTask;
        }
    }

    private sealed class RetryableTranslationPortalService : ITranslationPortalService
    {
        private readonly IReadOnlyList<TranslationDto> _translations =
        [
            new TranslationDto
            {
                Id = 41,
                PoiId = 3,
                LanguageCode = "vi",
                Title = "Phố Ẩm Thực Vĩnh Khánh",
                Description = "Trục món đêm nổi bật của Quận 4.",
                Story = "Một dải phố ăn khuya nổi tiếng.",
                Highlight = "Ẩm thực đêm",
                IsFallback = false,
                WorkflowStatus = TranslationWorkflowStatus.Source
            },
            new TranslationDto
            {
                Id = 42,
                PoiId = 3,
                LanguageCode = "en",
                Title = "Vinh Khanh food street",
                Description = "A strong late-night dining strip.",
                Story = "A famous after-dark corridor for street food.",
                Highlight = "Night food scene",
                IsFallback = false,
                WorkflowStatus = TranslationWorkflowStatus.Reviewed
            }
        ];

        public Task<IReadOnlyList<TranslationDto>> GetByPoiAsync(int poiId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TranslationDto>>(_translations.Where(item => item.PoiId == poiId).ToArray());
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
