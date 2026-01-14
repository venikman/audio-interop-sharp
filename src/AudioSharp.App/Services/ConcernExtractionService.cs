using System.Text.Json;
using System.Text.Json.Serialization;
using AudioSharp.App.Models;

namespace AudioSharp.App.Services;

public sealed class ConcernExtractionService : IConcernExtractionService
{
    private const string SystemPrompt =
        "You are a clinical data extraction assistant. Extract patient concerns from transcripts. " +
        "Do not invent details. If information is missing, use null.";

    private const string UserPromptTemplate =
        """
        Return ONLY a JSON object matching this schema:
        {
          "concerns": [
            {
              "summary": "short concern statement",
              "severity": "low|medium|high|unknown",
              "onset": "when it started",
              "duration": "how long it has lasted",
              "impact": "impact on daily life",
              "context": "additional context",
              "patientQuote": "verbatim quote"
            }
          ]
        }

        Transcript:
        <<TRANSCRIPT>>
        """;

    private readonly ITextCompletionClient _textClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConcernExtractionService(ITextCompletionClient textClient, JsonSerializerOptions jsonOptions)
    {
        _textClient = textClient;
        _jsonOptions = jsonOptions;
    }

    public async Task<ConcernExtractionResult> ExtractAsync(string transcript, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return new ConcernExtractionResult(string.Empty, []);
        }

        var messages = new List<ChatMessage>
        {
            new("system", SystemPrompt),
            new("user", UserPromptTemplate.Replace("<<TRANSCRIPT>>", transcript))
        };

        var completion = await _textClient
            .CompleteAsync(messages, cancellationToken)
            .ConfigureAwait(false);

        if (JsonParsingHelper.TryDeserializeJson<ConcernExtractionPayload>(completion, _jsonOptions, out var payload)
            && payload is not null)
        {
            var concerns = payload.Concerns?
                .Select(item => new ConcernItem(
                    item.Summary ?? string.Empty,
                    item.Severity,
                    item.Onset,
                    item.Duration,
                    item.Impact,
                    item.Context,
                    item.PatientQuote))
                .Where(item => !string.IsNullOrWhiteSpace(item.Summary))
                .ToList() ?? [];

            return new ConcernExtractionResult(transcript, concerns);
        }

        var fallback = new ConcernItem(
            transcript.Trim(),
            "unknown",
            null,
            null,
            null,
            "LLM response parsing failed",
            null);

        return new ConcernExtractionResult(transcript, [fallback]);
    }

    private sealed class ConcernExtractionPayload
    {
        [JsonPropertyName("concerns")]
        public List<ConcernItemContract>? Concerns { get; init; }
    }

    private sealed class ConcernItemContract
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; init; }

        [JsonPropertyName("severity")]
        public string? Severity { get; init; }

        [JsonPropertyName("onset")]
        public string? Onset { get; init; }

        [JsonPropertyName("duration")]
        public string? Duration { get; init; }

        [JsonPropertyName("impact")]
        public string? Impact { get; init; }

        [JsonPropertyName("context")]
        public string? Context { get; init; }

        [JsonPropertyName("patientQuote")]
        public string? PatientQuote { get; init; }
    }
}
