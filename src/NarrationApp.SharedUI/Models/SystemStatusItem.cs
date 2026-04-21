namespace NarrationApp.SharedUI.Models;

public sealed class SystemStatusItem
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public StatusTone Tone { get; init; } = StatusTone.Neutral;
}
