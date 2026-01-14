using AudioSharp.App.Models;

namespace AudioSharp.App.Services;

public interface IAudioTranscriptionService
{
    Task<TranscriptResult> TranscribeAsync(AudioInput audioInput, CancellationToken cancellationToken);
}
