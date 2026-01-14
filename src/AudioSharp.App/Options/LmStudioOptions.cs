namespace AudioSharp.App.Options;

public sealed class LmStudioOptions
{
    public const string SectionName = "LmStudio";

    public string BaseUrl { get; init; } = "http://localhost:1234/v1/";
    public string TextModel { get; init; } = "local-model";
}
