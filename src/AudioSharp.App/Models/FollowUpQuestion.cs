namespace AudioSharp.App.Models;

public sealed record FollowUpQuestion(
    int ConcernIndex,
    string ConcernSummary,
    string Field,
    string Question);
