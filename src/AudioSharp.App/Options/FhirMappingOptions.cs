namespace AudioSharp.App.Options;

public sealed class FhirMappingOptions
{
    public const string SectionName = "FhirMapping";

    public string ObservationCodeText { get; init; } = "Patient concern";
    public string? ObservationCodeSystem { get; init; }
    public string? ObservationCode { get; init; }
    public string? ObservationCodeDisplay { get; init; }
}
