using System.ComponentModel.DataAnnotations;

namespace AudioSharp.App.Options;

public sealed class RecordingOptions
{
    public const string SectionName = "Recording";

    [Range(5, 600)]
    public int MaxSeconds { get; init; } = 60;

    [Range(1_048_576, 104_857_600)]
    public long MaxAudioBytes { get; init; } = 25 * 1024 * 1024;
}
