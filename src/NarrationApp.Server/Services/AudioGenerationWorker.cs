using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class AudioGenerationWorker(
    IServiceScopeFactory scopeFactory,
    IAudioGenerationQueue queue,
    ILogger<AudioGenerationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverPendingWorkAsync(stoppingToken);

        await foreach (var item in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<IAudioGenerationProcessor>();
                await processor.ProcessAsync(item, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Audio generation worker failed for audio asset {AudioAssetId}, poi {PoiId}, language {LanguageCode}.",
                    item.AudioAssetId,
                    item.PoiId,
                    item.LanguageCode);
            }
        }
    }

    private async Task RecoverPendingWorkAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var items = await dbContext.AudioAssets
            .AsNoTracking()
            .Where(item =>
                item.SourceType == AudioSourceType.Tts
                && (item.Status == AudioStatus.Requested || item.Status == AudioStatus.Generating))
            .Select(item => new AudioGenerationWorkItem(item.Id, item.PoiId, item.LanguageCode, "standard"))
            .ToListAsync(stoppingToken);

        foreach (var item in items)
        {
            await queue.QueueAsync(item, stoppingToken);
        }
    }
}
