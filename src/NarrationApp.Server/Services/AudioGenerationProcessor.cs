using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class AudioGenerationProcessor(
    AppDbContext dbContext,
    IStorageService storageService,
    IGoogleTtsService googleTtsService,
    ILogger<AudioGenerationProcessor> logger) : IAudioGenerationProcessor
{
    public async Task ProcessAsync(AudioGenerationWorkItem item, CancellationToken cancellationToken = default)
    {
        var asset = await dbContext.AudioAssets
            .SingleOrDefaultAsync(audio => audio.Id == item.AudioAssetId, cancellationToken);

        if (asset is null || asset.SourceType != AudioSourceType.Tts)
        {
            return;
        }

        if (asset.Status is not AudioStatus.Requested and not AudioStatus.Generating)
        {
            return;
        }

        var translation = await dbContext.PoiTranslations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                record => record.PoiId == item.PoiId && record.LanguageCode == item.LanguageCode,
                cancellationToken);

        if (translation is null)
        {
            asset.Status = AudioStatus.Failed;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        asset.Status = AudioStatus.Generating;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var script = ResolveScript(translation);
            var bytes = await googleTtsService.GenerateAsync(script, item.LanguageCode, item.VoiceProfile, cancellationToken);
            await using var content = new MemoryStream(bytes);
            var (storagePath, url) = await storageService.SaveAsync($"tts_{item.PoiId}_{item.LanguageCode}.mp3", content, cancellationToken);

            asset.Provider = googleTtsService.ProviderName;
            asset.StoragePath = storagePath;
            asset.Url = string.IsNullOrWhiteSpace(url)
                ? $"/api/audio/{asset.Id}/stream"
                : url;
            asset.Status = AudioStatus.Ready;
            asset.DurationSeconds = 5;
            asset.GeneratedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            asset.Status = AudioStatus.Failed;
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogError(
                exception,
                "Background audio generation failed for audio asset {AudioAssetId}, poi {PoiId}, language {LanguageCode}.",
                item.AudioAssetId,
                item.PoiId,
                item.LanguageCode);
        }
    }

    private static string ResolveScript(PoiTranslation translation)
    {
        if (!string.IsNullOrWhiteSpace(translation.Story))
        {
            return translation.Story.Trim();
        }

        if (!string.IsNullOrWhiteSpace(translation.Description))
        {
            return translation.Description.Trim();
        }

        if (!string.IsNullOrWhiteSpace(translation.Highlight))
        {
            return translation.Highlight.Trim();
        }

        if (!string.IsNullOrWhiteSpace(translation.Title))
        {
            return translation.Title.Trim();
        }

        throw new InvalidOperationException("Translation does not contain any text to synthesize.");
    }
}
