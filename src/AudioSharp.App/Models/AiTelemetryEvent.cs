namespace AudioSharp.App.Models;

public sealed record AiTelemetryEvent(
    string Kind,
    string Provider,
    string Model,
    bool Success,
    TimeSpan Duration,
    DateTimeOffset OccurredAtUtc);
