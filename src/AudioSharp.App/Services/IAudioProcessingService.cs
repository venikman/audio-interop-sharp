using AudioSharp.App.Models;

namespace AudioSharp.App.Services;

public interface IAudioProcessingService
{
    Task<ProcessingResult> ProcessAsync(
        AudioInput audioInput,
        ProcessingContext context,
        CancellationToken cancellationToken);
}
