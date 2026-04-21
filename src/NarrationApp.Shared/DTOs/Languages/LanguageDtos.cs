using NarrationApp.Shared.Enums;

namespace NarrationApp.Shared.DTOs.Languages;

public sealed class ManagedLanguageDto
{
    public string Code { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string NativeName { get; init; } = string.Empty;

    public string FlagCode { get; init; } = string.Empty;

    public ManagedLanguageRole Role { get; init; }

    public bool IsActive { get; init; }

    public int TranslationCoverageCount { get; init; }

    public int TranslationCoverageTotal { get; init; }

    public int AudioCount { get; init; }
}

public sealed class CreateManagedLanguageRequest
{
    public string Code { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string NativeName { get; init; } = string.Empty;

    public string FlagCode { get; init; } = string.Empty;
}
