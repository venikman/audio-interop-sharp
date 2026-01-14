namespace AudioSharp.App.Options;

public enum TextProviderKind
{
    LmStudio,
    OpenRouter
}

public sealed class LlmProviderOptions
{
    public const string SectionName = "LlmProvider";

    public TextProviderKind TextProvider { get; init; } = TextProviderKind.LmStudio;
}
