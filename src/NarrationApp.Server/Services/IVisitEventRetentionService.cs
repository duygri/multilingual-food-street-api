namespace NarrationApp.Server.Services;

public interface IVisitEventRetentionService
{
    Task<int> PurgeExpiredAsync(DateTime referenceTimeUtc, CancellationToken cancellationToken = default);
}
