namespace AudioSharp.App.Models;

public sealed record AiUsageSnapshot(
    UsageCounter Audio,
    UsageCounter Text)
{
    public static AiUsageSnapshot Empty { get; } = new(
        new UsageCounter(0, 0, 0, null, null, null, null),
        new UsageCounter(0, 0, 0, null, null, null, null));
}

public sealed record UsageCounter(
    long Total,
    long Success,
    long Failure,
    TimeSpan? LastDuration,
    DateTimeOffset? LastAtUtc,
    string? LastProvider,
    string? LastModel);
