using AudioSharp.App.Models;
using AudioSharp.App.Models.Fhir;

namespace AudioSharp.App.Services;

public interface IFhirObservationMapper
{
    IReadOnlyList<FhirObservation> Map(
        IReadOnlyList<ConcernItem> concerns,
        ProcessingContext context,
        DateTimeOffset recordedAt);
}
