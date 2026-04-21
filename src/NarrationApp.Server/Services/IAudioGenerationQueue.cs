namespace NarrationApp.Server.Services;

public interface IAudioGenerationQueue
{
    ValueTask QueueAsync(AudioGenerationWorkItem item, CancellationToken cancellationToken = default);

    IAsyncEnumerable<AudioGenerationWorkItem> DequeueAllAsync(CancellationToken cancellationToken = default);
}
