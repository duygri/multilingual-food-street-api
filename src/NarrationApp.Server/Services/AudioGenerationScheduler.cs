using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class AudioGenerationScheduler(
    AppDbContext dbContext,
    IAudioGenerationQueue queue,
    IGoogleTtsService googleTtsService) : IAudioGenerationScheduler
{
    public async Task QueueFromTranslationAsync(int poiId, string languageCode, string voiceProfile = "standard", CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = languageCode.Trim().ToLowerInvariant();
        if (string.Equals(normalizedLanguage, AppConstants.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _ = await dbContext.PoiTranslations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.PoiId == poiId && item.LanguageCode == normalizedLanguage,
                cancellationToken)
            ?? throw new KeyNotFoundException("Saved translation was not found.");

        var asset = await dbContext.AudioAssets
            .SingleOrDefaultAsync(
                item => item.PoiId == poiId
                    && item.LanguageCode == normalizedLanguage
                    && item.SourceType == AudioSourceType.Tts,
                cancellationToken);

        if (asset is null)
        {
            asset = new AudioAsset
            {
                PoiId = poiId,
                LanguageCode = normalizedLanguage,
                SourceType = AudioSourceType.Tts
            };

            dbContext.AudioAssets.Add(asset);
        }

        asset.Provider = googleTtsService.ProviderName;
        asset.Status = AudioStatus.Requested;
        asset.GeneratedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await queue.QueueAsync(new AudioGenerationWorkItem(asset.Id, poiId, normalizedLanguage, voiceProfile), cancellationToken);
    }
}
