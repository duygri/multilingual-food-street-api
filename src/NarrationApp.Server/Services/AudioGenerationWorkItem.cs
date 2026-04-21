namespace NarrationApp.Server.Services;

public sealed record AudioGenerationWorkItem(int AudioAssetId, int PoiId, string LanguageCode, string VoiceProfile);
