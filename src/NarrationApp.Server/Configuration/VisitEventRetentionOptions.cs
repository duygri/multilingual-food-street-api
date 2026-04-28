namespace NarrationApp.Server.Configuration;

public sealed class VisitEventRetentionOptions
{
    public const string SectionName = "VisitEventRetention";

    public bool Enabled { get; init; }

    public int RawEventRetentionDays { get; init; } = 30;

    public int SweepIntervalHours { get; init; } = 24;

    public int BatchSize { get; init; } = 1_000;

    public TimeSpan RetentionWindow => TimeSpan.FromDays(Math.Clamp(RawEventRetentionDays, 1, 365));

    public TimeSpan SweepInterval => TimeSpan.FromHours(Math.Clamp(SweepIntervalHours, 1, 168));

    public int NormalizedBatchSize => Math.Clamp(BatchSize, 100, 10_000);
}
