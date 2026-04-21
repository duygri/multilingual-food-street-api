namespace NarrationApp.Server.Services;

public interface IAudioGenerationScheduler
{
    Task QueueFromTranslationAsync(int poiId, string languageCode, string voiceProfile = "standard", CancellationToken cancellationToken = default);
}
