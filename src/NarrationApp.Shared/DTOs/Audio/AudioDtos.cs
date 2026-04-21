using NarrationApp.Shared.Enums;

namespace NarrationApp.Shared.DTOs.Audio;

public sealed class AudioDto
{
    public int Id { get; init; }

    public int PoiId { get; init; }

    public string LanguageCode { get; init; } = string.Empty;

    public AudioSourceType SourceType { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string StoragePath { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public AudioStatus Status { get; init; }

    public int DurationSeconds { get; init; }

    public DateTime? GeneratedAtUtc { get; init; }
}

public sealed class UploadAudioRequest
{
    public int PoiId { get; init; }

    public string LanguageCode { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;
}

public sealed class TtsGenerateRequest
{
    public int PoiId { get; init; }

    public string LanguageCode { get; init; } = string.Empty;

    public string Script { get; init; } = string.Empty;

    public string VoiceProfile { get; init; } = "standard";
}

public sealed class UpdateAudioRequest
{
    public string LanguageCode { get; init; } = string.Empty;

    public string Provider { get; init; } = string.Empty;

    public string StoragePath { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public AudioStatus Status { get; init; }

    public int DurationSeconds { get; init; }
}

public sealed class GenerateAudioFromTranslationRequest
{
    public int PoiId { get; init; }

    public string LanguageCode { get; init; } = string.Empty;

    public string VoiceProfile { get; init; } = "standard";
}
