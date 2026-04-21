using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Audio;

public sealed class AudioGenerationSchedulerTests
{
    [Fact]
    public async Task QueueFromTranslationAsync_creates_requested_audio_asset_and_enqueues_work_item()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        dbContext.PoiTranslations.Add(new PoiTranslation
        {
            PoiId = poi.Id,
            LanguageCode = "en",
            Title = "English title",
            Description = "English description",
            Story = "English story",
            Highlight = "English highlight"
        });
        await dbContext.SaveChangesAsync();

        var queue = new RecordingAudioGenerationQueue();
        var sut = new AudioGenerationScheduler(dbContext, queue, new MockGoogleTtsService());

        await sut.QueueFromTranslationAsync(poi.Id, "en");

        var asset = await dbContext.AudioAssets.SingleAsync(item =>
            item.PoiId == poi.Id
            && item.LanguageCode == "en"
            && item.SourceType == AudioSourceType.Tts);

        Assert.Equal(AudioStatus.Requested, asset.Status);
        Assert.Equal("mock-google-tts", asset.Provider);
        Assert.Single(queue.Items);
        Assert.Equal(asset.Id, queue.Items[0].AudioAssetId);
        Assert.Equal("en", queue.Items[0].LanguageCode);
    }

    private sealed class RecordingAudioGenerationQueue : IAudioGenerationQueue
    {
        public List<AudioGenerationWorkItem> Items { get; } = [];

        public async IAsyncEnumerable<AudioGenerationWorkItem> DequeueAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask QueueAsync(AudioGenerationWorkItem item, CancellationToken cancellationToken = default)
        {
            Items.Add(item);
            return ValueTask.CompletedTask;
        }
    }
}
