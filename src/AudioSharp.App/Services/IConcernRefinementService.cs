using AudioSharp.App.Models;

namespace AudioSharp.App.Services;

public interface IConcernRefinementService
{
    Task<IReadOnlyList<ConcernItem>> ApplyAnswersAsync(
        string transcript,
        IReadOnlyList<ConcernItem> concerns,
        IReadOnlyList<FollowUpAnswer> answers,
        CancellationToken cancellationToken);
}
