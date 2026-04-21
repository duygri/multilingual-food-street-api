using System.Text;

namespace NarrationApp.Server.Services;

public sealed class MockGoogleTtsService : IGoogleTtsService
{
    public string ProviderName => "mock-google-tts";

    public Task<byte[]> GenerateAsync(string script, string languageCode, string voiceProfile, CancellationToken cancellationToken = default)
    {
        var payload = Encoding.UTF8.GetBytes($"ID3MOCK-{languageCode}-{voiceProfile}-{script}");
        return Task.FromResult(payload);
    }
}
