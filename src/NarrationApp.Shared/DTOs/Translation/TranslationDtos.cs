using NarrationApp.Shared.Enums;

namespace NarrationApp.Shared.DTOs.Translation;

public sealed class TranslationDto
{
    public int Id { get; init; }

    public int PoiId { get; init; }

    public string LanguageCode { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Story { get; init; } = string.Empty;

    public string Highlight { get; init; } = string.Empty;

    public bool IsFallback { get; init; }

    public TranslationWorkflowStatus WorkflowStatus { get; init; }
}

public sealed class CreateTranslationRequest
{
    public int PoiId { get; init; }

    public string LanguageCode { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Story { get; init; } = string.Empty;

    public string Highlight { get; init; } = string.Empty;

    public bool IsFallback { get; init; }
}
