using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Data.Entities;

public sealed class ManagedLanguage
{
    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string NativeName { get; set; } = string.Empty;

    public string FlagCode { get; set; } = string.Empty;

    public ManagedLanguageRole Role { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
