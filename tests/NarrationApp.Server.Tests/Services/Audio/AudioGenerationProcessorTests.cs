using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Audio;

public sealed class AudioGenerationProcessorTests
{
    [Fact]
    public async Task ProcessAsync_turns_requested_translation_audio_into_ready_asset()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        dbContext.PoiTranslations.Add(new PoiTranslation
        {
            PoiId = poi.Id,
            LanguageCode = "en",
            Title = "English title",
            Description = "English description",
            Story = "English story for background audio",
            Highlight = "English highlight"
        });

        var asset = new AudioAsset
        {
            PoiId = poi.Id,
            LanguageCode = "en",
            SourceType = AudioSourceType.Tts,
            Provider = "mock-google-tts",
            Status = AudioStatus.Requested
        };
        dbContext.AudioAssets.Add(asset);
        await dbContext.SaveChangesAsync();

        var storageRoot = TestStorageRoot.Create();
        var ttsService = new RecordingGoogleTtsService();
        var sut = new AudioGenerationProcessor(dbContext, new MockStorageService(storageRoot), ttsService, NullLogger<AudioGenerationProcessor>.Instance);

        await sut.ProcessAsync(new AudioGenerationWorkItem(asset.Id, poi.Id, "en", "standard"));

        var persisted = await dbContext.AudioAssets.SingleAsync(item => item.Id == asset.Id);
        Assert.Equal(AudioStatus.Ready, persisted.Status);
        Assert.Equal("English story for background audio", ttsService.LastScript);
        Assert.Equal("en", ttsService.LastLanguageCode);
        Assert.True(File.Exists(persisted.StoragePath));
        Assert.False(string.IsNullOrWhiteSpace(persisted.Url));
    }

    private sealed class RecordingGoogleTtsService : IGoogleTtsService
    {
        public string ProviderName => "recording-google-tts";

        public string? LastScript { get; private set; }

        public string? LastLanguageCode { get; private set; }

        public string? LastVoiceProfile { get; private set; }

        public Task<byte[]> GenerateAsync(string script, string languageCode, string voiceProfile, CancellationToken cancellationToken = default)
        {
            LastScript = script;
            LastLanguageCode = languageCode;
            LastVoiceProfile = voiceProfile;
            return Task.FromResult(new byte[] { 1, 2, 3, 4, 5 });
        }
    }
}
