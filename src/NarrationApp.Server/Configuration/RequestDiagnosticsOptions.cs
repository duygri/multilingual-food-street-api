namespace NarrationApp.Server.Configuration;

public sealed class RequestDiagnosticsOptions
{
    public const string SectionName = "RequestDiagnostics";

    public int SlowRequestThresholdMs { get; init; } = 1000;
}
