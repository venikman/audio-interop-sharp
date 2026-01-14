namespace AudioSharp.App.Options;

public sealed class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://openrouter.ai/api/v1/";
    public string AudioModel { get; init; } = "google/gemini-2.5-flash-lite";
    public string TextModel { get; init; } = "google/gemini-2.5-flash-lite";
    public string? AppName { get; init; }
    public string? AppUrl { get; init; }
}
