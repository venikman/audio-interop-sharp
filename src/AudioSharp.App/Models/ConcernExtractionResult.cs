namespace AudioSharp.App.Models;

public sealed record ConcernExtractionResult(
    string Transcript,
    IReadOnlyList<ConcernItem> Concerns);
