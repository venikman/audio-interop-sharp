using AudioSharp.App.Models;

namespace AudioSharp.App.Services;

public interface IConcernExtractionService
{
    Task<ConcernExtractionResult> ExtractAsync(string transcript, CancellationToken cancellationToken);
}
