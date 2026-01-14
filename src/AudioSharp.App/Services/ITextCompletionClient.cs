using AudioSharp.App.Models;

namespace AudioSharp.App.Services;

public interface ITextCompletionClient
{
    Task<string> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken);
}
