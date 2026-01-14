using AudioSharp.App.Models.Fhir;

namespace AudioSharp.App.Models;

public sealed record ProcessingResult(
    string Transcript,
    IReadOnlyList<ConcernItem> Concerns,
    IReadOnlyList<FhirObservation> Observations,
    FhirBundle Bundle);
