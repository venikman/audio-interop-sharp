using AudioSharp.App.Models.Fhir;

namespace AudioSharp.App.Services;

public sealed class FhirBundleBuilder : IFhirBundleBuilder
{
    public FhirBundle Build(IReadOnlyList<FhirObservation> observations)
    {
        var bundle = new FhirBundle();
        foreach (var observation in observations)
        {
            bundle.Entry.Add(new FhirBundleEntry { Resource = observation });
        }

        return bundle;
    }
}
