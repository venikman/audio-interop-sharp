using System.Text.Json.Serialization;

namespace AudioSharp.App.Models.Fhir;

public sealed class FhirReference
{
    [JsonPropertyName("reference")]
    public string? Reference { get; init; }

    [JsonPropertyName("display")]
    public string? Display { get; init; }
}
