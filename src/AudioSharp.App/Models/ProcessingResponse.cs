namespace AudioSharp.App.Models;

public sealed record ProcessingResponse(
    string Transcript,
    IReadOnlyList<ConcernItem> Concerns,
    string BundleJson);
