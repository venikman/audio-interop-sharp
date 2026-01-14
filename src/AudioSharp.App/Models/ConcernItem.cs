namespace AudioSharp.App.Models;

public sealed record ConcernItem(
    string Summary,
    string? Severity,
    string? Onset,
    string? Duration,
    string? Impact,
    string? Context,
    string? PatientQuote);
