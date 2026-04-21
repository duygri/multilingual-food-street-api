namespace NarrationApp.Server.Services;

public sealed class MockGoogleTranslationService : IGoogleTranslationService
{
    public Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"[AUTO] {text}");
    }
}
