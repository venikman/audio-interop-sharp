using System.ComponentModel.DataAnnotations;

namespace AudioSharp.App.Options;

public sealed class RecordingOptions
{
    public const string SectionName = "Recording";

    [Range(5, 600)]
    public int MaxSeconds { get; init; } = 60;
}
