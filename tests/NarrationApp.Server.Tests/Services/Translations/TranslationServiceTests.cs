using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Translations;

public sealed class TranslationServiceTests
{
    [Fact]
    public async Task AutoTranslateAsync_creates_prefixed_mock_translation_for_target_language_without_enqueuing_audio_generation()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var sut = new TranslationService(dbContext, new MockGoogleTranslationService());

        var result = await sut.AutoTranslateAsync(poi.Id, "en");

        Assert.Equal("en", result.LanguageCode);
        Assert.StartsWith("[AUTO] ", result.Title);
        Assert.StartsWith("[AUTO] ", result.Description);
        Assert.StartsWith("[AUTO] ", result.Story);
        Assert.Equal(TranslationWorkflowStatus.AutoTranslated, result.WorkflowStatus);
    }

    [Fact]
    public async Task UpsertAsync_marks_vietnamese_as_source_and_non_vietnamese_manual_save_as_reviewed_without_enqueuing_audio_generation()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var sut = new TranslationService(dbContext, new MockGoogleTranslationService());

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
    }

    [Fact]
    public void TranslationService_does_not_reference_audio_generation_scheduler()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var sourcePath = Path.Combine(projectRoot, "src", "NarrationApp.Server", "Services", "TranslationService.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("IAudioGenerationScheduler", source, StringComparison.Ordinal);
        Assert.DoesNotContain("QueueFromTranslationAsync", source, StringComparison.Ordinal);
    }
}
