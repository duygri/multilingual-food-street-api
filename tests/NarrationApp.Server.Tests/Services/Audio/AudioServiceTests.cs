using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Audio;

public sealed class AudioServiceTests
{
    [Fact]
    public async Task UploadAsync_persists_local_audio_asset_for_owner_poi()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var storageRoot = TestStorageRoot.Create();
        var sut = new AudioService(dbContext, new MockStorageService(storageRoot), new MockGoogleTtsService());

        await using var audioStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var result = await sut.UploadAsync(
            poi.OwnerId,
            UserRole.PoiOwner,
            new UploadAudioRequest
            {
                PoiId = poi.Id,
                LanguageCode = AppConstants.DefaultLanguage,
                FileName = "sample.mp3"
            },
            audioStream);

        Assert.Equal(poi.Id, result.PoiId);
        Assert.Equal(AudioSourceType.Recorded, result.SourceType);
        Assert.Equal(AudioStatus.Ready, result.Status);
        Assert.True(File.Exists(result.StoragePath));
    }

    [Fact]
    public async Task GenerateTtsAsync_creates_ready_audio_asset_using_mock_provider()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var storageRoot = TestStorageRoot.Create();
        var googleTtsService = new RecordingGoogleTtsService();
        var sut = new AudioService(dbContext, new MockStorageService(storageRoot), googleTtsService);

        var result = await sut.GenerateTtsAsync(
            poi.OwnerId,
            UserRole.PoiOwner,
            new TtsGenerateRequest
            {
                PoiId = poi.Id,
                LanguageCode = "en",
                Script = "Hello from the narration app",
                VoiceProfile = "neural2"
            });

        Assert.Equal(AudioSourceType.Tts, result.SourceType);
        Assert.Equal(AudioStatus.Ready, result.Status);
        Assert.Equal("recording-google-tts", result.Provider);
        Assert.Equal("Hello from the narration app", googleTtsService.LastScript);
        Assert.Equal("en", googleTtsService.LastLanguageCode);
        Assert.Equal("neural2", googleTtsService.LastVoiceProfile);
        Assert.True(File.Exists(result.StoragePath));
    }

    [Fact]
    public async Task GenerateFromTranslationAsync_uses_saved_translation_story_for_target_language()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var storageRoot = TestStorageRoot.Create();
        var googleTtsService = new RecordingGoogleTtsService();
        var translationService = new TranslationService(dbContext, new MockGoogleTranslationService());
        var sut = new AudioService(dbContext, new MockStorageService(storageRoot), googleTtsService);

        await translationService.UpsertAsync(new CreateTranslationRequest
        {
            PoiId = poi.Id,
            LanguageCode = "en",
            Title = "English title",
            Description = "English description",
            Story = "English story for TTS",
            Highlight = "English highlight",
            IsFallback = false
        });

        var result = await sut.GenerateFromTranslationAsync(
            poi.OwnerId,
            UserRole.PoiOwner,
            new GenerateAudioFromTranslationRequest
            {
                PoiId = poi.Id,
                LanguageCode = "en",
                VoiceProfile = "wavenet"
            });

        Assert.Equal(AudioSourceType.Tts, result.SourceType);
        Assert.Equal("English story for TTS", googleTtsService.LastScript);
        Assert.Equal("en", googleTtsService.LastLanguageCode);
        Assert.Equal("wavenet", googleTtsService.LastVoiceProfile);
        Assert.True(File.Exists(result.StoragePath));
    }

    [Fact]
    public async Task UploadAsync_uses_internal_stream_url_when_storage_provider_has_no_public_url()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var sut = new AudioService(dbContext, new StubStorageService(("audio/object.mp3", string.Empty)), new MockGoogleTtsService());

        await using var audioStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await sut.UploadAsync(
            poi.OwnerId,
            UserRole.PoiOwner,
            new UploadAudioRequest
            {
                PoiId = poi.Id,
                LanguageCode = AppConstants.DefaultLanguage,
                FileName = "sample.mp3"
            },
            audioStream);

        Assert.Equal($"/api/audio/{result.Id}/stream", result.Url);
    }

    private sealed class StubStorageService((string StoragePath, string Url) saveResult) : IStorageService
    {
        public string ProviderName => "stub-storage";

        public Task<(string StoragePath, string Url)> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(saveResult);
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            Stream stream = new MemoryStream();
            return Task.FromResult(stream);
        }

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
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
            return Task.FromResult(new byte[] { 1, 2, 3, 4 });
        }
    }
}
