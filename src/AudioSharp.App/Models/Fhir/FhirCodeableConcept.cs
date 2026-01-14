using System.Text.Json.Serialization;

namespace AudioSharp.App.Models.Fhir;

public sealed class FhirCodeableConcept
{
    [JsonPropertyName("coding")]
    public List<FhirCoding>? Coding { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}
