namespace NarrationApp.SharedUI.Models;

public sealed class ShellNavItem
{
    public string Group { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Href { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? IconGlyph { get; init; }

    public string? BadgeText { get; init; }

    public bool MatchAll { get; init; }
}
