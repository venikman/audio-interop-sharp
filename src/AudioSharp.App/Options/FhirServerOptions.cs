namespace AudioSharp.App.Options;

public sealed class FhirServerOptions
{
    public const string SectionName = "FhirServer";

    public string? BaseUrl { get; init; }
    public string BundleEndpoint { get; init; } = "Bundle";
    public string? BearerToken { get; init; }
}
