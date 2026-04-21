namespace NarrationApp.SharedUI.Models;

public sealed class HeroAction
{
    public string Label { get; init; } = string.Empty;

    public string Href { get; init; } = "#";

    public bool IsPrimary { get; init; } = true;
}
