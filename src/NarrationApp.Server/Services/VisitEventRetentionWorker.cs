using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;

namespace NarrationApp.Server.Services;

public sealed class VisitEventRetentionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<VisitEventRetentionOptions> options,
    ILogger<VisitEventRetentionWorker> logger) : BackgroundService
{
    private readonly VisitEventRetentionOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Visit event retention worker is disabled.");
            return;
        }

        try
        {
            await RunSweepAsync(stoppingToken);

            using var timer = new PeriodicTimer(_options.SweepInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunSweepAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var retentionService = scope.ServiceProvider.GetRequiredService<IVisitEventRetentionService>();
            await retentionService.PurgeExpiredAsync(DateTime.UtcNow, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Visit event retention sweep failed.");
        }
    }
}
