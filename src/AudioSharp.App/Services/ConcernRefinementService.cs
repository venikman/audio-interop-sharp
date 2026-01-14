using System.Text.Json;
using System.Text.Json.Serialization;
using AudioSharp.App.Models;

namespace AudioSharp.App.Services;

public sealed class ConcernRefinementService : IConcernRefinementService
{
    private const string SystemPrompt =
        "You refine extracted concerns using follow-up answers. " +
        "Do not invent details. Keep the same concern count and order.";

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

        Rules:
        - Do not add or remove concerns.
        - Keep original values if an answer does not supply a value.
        - Do not invent details.

        Transcript:
        <<TRANSCRIPT>>

        Original concerns:
        <<CONCERNS>>

        Follow-up answers:
        <<ANSWERS>>
        """;

    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "severity",
        "onset",
        "duration",
        "impact",
        "context",
        "patientQuote"
    };

    private readonly ITextCompletionClient _textClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConcernRefinementService(ITextCompletionClient textClient, JsonSerializerOptions jsonOptions)
    {
        _textClient = textClient;
        _jsonOptions = jsonOptions;
    }

    public async Task<IReadOnlyList<ConcernItem>> ApplyAnswersAsync(
        string transcript,
        IReadOnlyList<ConcernItem> concerns,
        IReadOnlyList<FollowUpAnswer> answers,
        CancellationToken cancellationToken)
    {
        if (concerns.Count == 0)
        {
            return [];
        }

        var normalizedAnswers = NormalizeAnswers(answers);
        if (normalizedAnswers.Count == 0)
        {
            return concerns;
        }

        var concernsJson = JsonSerializer.Serialize(concerns, _jsonOptions);
        var answersJson = JsonSerializer.Serialize(normalizedAnswers, _jsonOptions);
        var prompt = UserPromptTemplate
            .Replace("<<TRANSCRIPT>>", transcript ?? string.Empty)
            .Replace("<<CONCERNS>>", concernsJson)
            .Replace("<<ANSWERS>>", answersJson);

        var messages = new List<ChatMessage>
        {
            new("system", SystemPrompt),
            new("user", prompt)
        };

        var completion = await _textClient
            .CompleteAsync(messages, cancellationToken)
            .ConfigureAwait(false);

        if (JsonParsingHelper.TryDeserializeJson<ConcernPayload>(completion, _jsonOptions, out var payload)
            && payload?.Concerns is { Count: > 0 })
        {
            return MergeResults(concerns, payload.Concerns);
        }

        return ApplyAnswersFallback(concerns, normalizedAnswers);
    }

    private static IReadOnlyList<FollowUpAnswer> NormalizeAnswers(IReadOnlyList<FollowUpAnswer> answers)
    {
        if (answers.Count == 0)
        {
            return [];
        }

        var normalized = new List<FollowUpAnswer>();
        foreach (var answer in answers)
        {
            if (answer.ConcernIndex < 0 || string.IsNullOrWhiteSpace(answer.Answer))
            {
                continue;
            }

            var normalizedField = NormalizeField(answer.Field);
            if (string.IsNullOrWhiteSpace(normalizedField))
            {
                continue;
            }

            normalized.Add(answer with
            {
                Field = normalizedField,
                Answer = answer.Answer.Trim()
            });
        }

        return normalized;
    }

    private static string NormalizeField(string? field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return string.Empty;
        }

        var normalized = field.Trim();
        return AllowedFields.Contains(normalized) ? normalized.ToLowerInvariant() : string.Empty;
    }

    private static IReadOnlyList<ConcernItem> MergeResults(
        IReadOnlyList<ConcernItem> original,
        IReadOnlyList<ConcernItemContract> updated)
    {
        var results = new List<ConcernItem>(original.Count);

        for (var i = 0; i < original.Count; i++)
        {
            var baseItem = original[i];
            var updatedItem = i < updated.Count ? updated[i] : null;
            if (updatedItem is null)
            {
                results.Add(baseItem);
                continue;
            }

            results.Add(new ConcernItem(
                string.IsNullOrWhiteSpace(updatedItem.Summary) ? baseItem.Summary : updatedItem.Summary,
                string.IsNullOrWhiteSpace(updatedItem.Severity) ? baseItem.Severity : updatedItem.Severity,
                string.IsNullOrWhiteSpace(updatedItem.Onset) ? baseItem.Onset : updatedItem.Onset,
                string.IsNullOrWhiteSpace(updatedItem.Duration) ? baseItem.Duration : updatedItem.Duration,
                string.IsNullOrWhiteSpace(updatedItem.Impact) ? baseItem.Impact : updatedItem.Impact,
                string.IsNullOrWhiteSpace(updatedItem.Context) ? baseItem.Context : updatedItem.Context,
                string.IsNullOrWhiteSpace(updatedItem.PatientQuote) ? baseItem.PatientQuote : updatedItem.PatientQuote));
        }

        return results;
    }

    private static IReadOnlyList<ConcernItem> ApplyAnswersFallback(
        IReadOnlyList<ConcernItem> concerns,
        IReadOnlyList<FollowUpAnswer> answers)
    {
        var lookup = new Dictionary<(int Index, string Field), string>();
        foreach (var answer in answers)
        {
            lookup[(answer.ConcernIndex, answer.Field)] = answer.Answer;
        }

        var updated = new List<ConcernItem>(concerns.Count);
        for (var i = 0; i < concerns.Count; i++)
        {
            var concern = concerns[i];
            lookup.TryGetValue((i, "severity"), out var severity);
            lookup.TryGetValue((i, "onset"), out var onset);
            lookup.TryGetValue((i, "duration"), out var duration);
            lookup.TryGetValue((i, "impact"), out var impact);
            lookup.TryGetValue((i, "context"), out var context);
            lookup.TryGetValue((i, "patientQuote"), out var patientQuote);

            updated.Add(new ConcernItem(
                concern.Summary,
                severity ?? concern.Severity,
                onset ?? concern.Onset,
                duration ?? concern.Duration,
                impact ?? concern.Impact,
                context ?? concern.Context,
                patientQuote ?? concern.PatientQuote));
        }

        return updated;
    }

    private sealed class ConcernPayload
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
