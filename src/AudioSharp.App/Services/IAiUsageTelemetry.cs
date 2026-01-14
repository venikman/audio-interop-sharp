using AudioSharp.App.Models;

namespace AudioSharp.App.Services;

public interface IAiUsageTelemetry
{
    event Action? Changed;

    AiUsageSnapshot GetSnapshot();

    IReadOnlyList<AiTelemetryEvent> GetRecentEvents();

    void RecordAudioTranscription(string provider, string model, bool success, TimeSpan duration);

    void RecordTextCompletion(string provider, string model, bool success, TimeSpan duration);
}
