using System.Text.Json.Serialization;

namespace AudioSharp.App.Models.Fhir;

public sealed class FhirObservation
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; init; } = "Observation";

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "final";

    [JsonPropertyName("category")]
    public List<FhirCodeableConcept>? Category { get; init; }

    [JsonPropertyName("code")]
    public FhirCodeableConcept Code { get; init; } = new();

    [JsonPropertyName("subject")]
    public FhirReference? Subject { get; init; }

    [JsonPropertyName("effectiveDateTime")]
    public string? EffectiveDateTime { get; init; }

    [JsonPropertyName("valueString")]
    public string? ValueString { get; init; }

    [JsonPropertyName("note")]
    public List<FhirAnnotation>? Note { get; init; }
}
