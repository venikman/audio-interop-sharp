using AudioSharp.App.Models.Fhir;

namespace AudioSharp.App.Services;

public interface IFhirBundleBuilder
{
    FhirBundle Build(IReadOnlyList<FhirObservation> observations);
}
