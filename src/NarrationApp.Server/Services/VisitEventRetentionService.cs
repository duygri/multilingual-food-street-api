using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;
using NarrationApp.Server.Data;

namespace NarrationApp.Server.Services;

public sealed class VisitEventRetentionService(
    AppDbContext dbContext,
    IOptions<VisitEventRetentionOptions> options,
    ILogger<VisitEventRetentionService> logger) : IVisitEventRetentionService
{
    private readonly VisitEventRetentionOptions _options = options.Value;

    public async Task<int> PurgeExpiredAsync(DateTime referenceTimeUtc, CancellationToken cancellationToken = default)
    {
        var cutoffUtc = referenceTimeUtc.ToUniversalTime().Subtract(_options.RetentionWindow);
        var deletedCount = 0;

        while (true)
        {
            var expiredEvents = await dbContext.VisitEvents
                .Where(item => item.CreatedAt < cutoffUtc)
                .OrderBy(item => item.CreatedAt)
                .Take(_options.NormalizedBatchSize)
                .ToListAsync(cancellationToken);

            if (expiredEvents.Count == 0)
            {
                break;
            }

            dbContext.VisitEvents.RemoveRange(expiredEvents);
            deletedCount += expiredEvents.Count;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (deletedCount > 0)
        {
            logger.LogInformation(
                "Purged {DeletedCount} visit events older than {CutoffUtc}.",
                deletedCount,
                cutoffUtc);
        }

        return deletedCount;
    }
}
