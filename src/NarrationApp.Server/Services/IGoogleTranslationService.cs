namespace NarrationApp.Server.Services;

public interface IGoogleTranslationService
{
    Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken = default);
}
