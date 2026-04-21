using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Translations;

public sealed class TranslationServiceTests
{
    [Fact]
    public async Task AutoTranslateAsync_creates_prefixed_mock_translation_for_target_language_and_enqueues_audio_generation()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var audioScheduler = new RecordingAudioGenerationScheduler();
        var sut = new TranslationService(dbContext, new MockGoogleTranslationService(), audioScheduler);

        var result = await sut.AutoTranslateAsync(poi.Id, "en");

        Assert.Equal("en", result.LanguageCode);
        Assert.StartsWith("[AUTO] ", result.Title);
        Assert.StartsWith("[AUTO] ", result.Description);
        Assert.StartsWith("[AUTO] ", result.Story);
        Assert.Equal(TranslationWorkflowStatus.AutoTranslated, result.WorkflowStatus);
        Assert.Single(audioScheduler.Requests);
        Assert.Equal((poi.Id, "en", "standard"), audioScheduler.Requests[0]);
    }

    [Fact]
    public async Task UpsertAsync_marks_vietnamese_as_source_and_non_vietnamese_manual_save_as_reviewed_and_only_queues_non_vietnamese_audio()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var audioScheduler = new RecordingAudioGenerationScheduler();
        var sut = new TranslationService(dbContext, new MockGoogleTranslationService(), audioScheduler);

        var vietnamese = await sut.UpsertAsync(new CreateTranslationRequest
        {
            PoiId = poi.Id,
            LanguageCode = "vi",
            Title = "Tieu de VI",
            Description = "Mo ta VI",
            Story = "Noi dung VI",
            Highlight = "Diem nhan VI",
            IsFallback = false
        });

        var english = await sut.UpsertAsync(new CreateTranslationRequest
        {
            PoiId = poi.Id,
            LanguageCode = "en",
            Title = "Reviewed EN",
            Description = "Reviewed description",
            Story = "Reviewed story",
            Highlight = "Reviewed highlight",
            IsFallback = false
        });

        Assert.Equal(TranslationWorkflowStatus.Source, vietnamese.WorkflowStatus);
        Assert.Equal(TranslationWorkflowStatus.Reviewed, english.WorkflowStatus);
        Assert.Single(audioScheduler.Requests);
        Assert.Equal((poi.Id, "en", "standard"), audioScheduler.Requests[0]);
    }

    private sealed class RecordingAudioGenerationScheduler : IAudioGenerationScheduler
    {
        public List<(int PoiId, string LanguageCode, string VoiceProfile)> Requests { get; } = [];

        public Task QueueFromTranslationAsync(int poiId, string languageCode, string voiceProfile = "standard", CancellationToken cancellationToken = default)
        {
            Requests.Add((poiId, languageCode, voiceProfile));
            return Task.CompletedTask;
        }
    }
}
