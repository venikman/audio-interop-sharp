using System.Text.Json.Serialization;

namespace AudioSharp.App.Models.Fhir;

public sealed class FhirCoding
{
    [JsonPropertyName("system")]
    public string? System { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("display")]
    public string? Display { get; init; }
}
