using AudioSharp.App.Models;

namespace AudioSharp.App.Services;

public interface IFollowUpQuestionService
{
    Task<IReadOnlyList<FollowUpQuestion>> GenerateAsync(
        string transcript,
        IReadOnlyList<ConcernItem> concerns,
        CancellationToken cancellationToken);
}
