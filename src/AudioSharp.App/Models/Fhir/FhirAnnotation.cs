using System.Text.Json.Serialization;

namespace AudioSharp.App.Models.Fhir;

public sealed class FhirAnnotation
{
    public FhirAnnotation(string text)
    {
        Text = text;
    }

    [JsonPropertyName("text")]
    public string Text { get; init; }
}
