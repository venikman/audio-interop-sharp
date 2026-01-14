using System.Text.Json.Serialization;

namespace AudioSharp.App.Models.Fhir;

public sealed class FhirBundle
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; init; } = "Bundle";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "collection";

    [JsonPropertyName("entry")]
    public List<FhirBundleEntry> Entry { get; init; } = [];
}

public sealed class FhirBundleEntry
{
    [JsonPropertyName("resource")]
    public FhirObservation Resource { get; init; } = new();
}
