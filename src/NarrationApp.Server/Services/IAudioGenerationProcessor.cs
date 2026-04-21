namespace NarrationApp.Server.Services;

public interface IAudioGenerationProcessor
{
    Task ProcessAsync(AudioGenerationWorkItem item, CancellationToken cancellationToken = default);
}
