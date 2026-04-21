using System.Threading.Channels;

namespace NarrationApp.Server.Services;

public sealed class InMemoryAudioGenerationQueue : IAudioGenerationQueue
{
    private readonly Channel<AudioGenerationWorkItem> _channel = Channel.CreateUnbounded<AudioGenerationWorkItem>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask QueueAsync(AudioGenerationWorkItem item, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(item, cancellationToken);
    }

    public IAsyncEnumerable<AudioGenerationWorkItem> DequeueAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
