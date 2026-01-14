namespace AudioSharp.App.Models;

public sealed record FollowUpAnswer(
    int ConcernIndex,
    string Field,
    string Answer);
