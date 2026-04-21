namespace NarrationApp.Server.Services;

public interface IGoogleTtsService
{
    string ProviderName { get; }

    Task<byte[]> GenerateAsync(string script, string languageCode, string voiceProfile, CancellationToken cancellationToken = default);
}
