using System.Text.Json;
using System.Text.Json.Serialization;
using AudioSharp.App.Models;

namespace AudioSharp.App.Services;

public sealed class FollowUpQuestionService : IFollowUpQuestionService
{
    private const int MaxQuestions = 3;

    private const string SystemPrompt =
        "You are a clinical follow-up assistant. Generate concise questions to fill missing concern details. " +
        "Do not invent details or ask about fields that are already present.";

    private const string UserPromptTemplate =
        """
        Return ONLY a JSON object matching this schema:
        {
          "questions": [
            {
              "concernIndex": 0,
              "field": "severity|onset|duration|impact|context",
              "question": "short follow-up question"
            }
          ]
        }

        Rules:
        - Ask at most 3 questions total.
        - Use only the listed fields.
        - Do not ask about fields that are not missing.

        Missing fields by concern (index, summary, missingFields):
        <<MISSING_FIELDS>>

        Transcript:
        <<TRANSCRIPT>>
        """;

    private static readonly string[] OrderedFields =
    [
        "severity",
        "onset",
        "duration",
        "impact",
        "context"
    ];

    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "severity",
        "onset",
        "duration",
        "impact",
        "context"
    };

    private readonly ITextCompletionClient _textClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public FollowUpQuestionService(ITextCompletionClient textClient, JsonSerializerOptions jsonOptions)
    {
        _textClient = textClient;
        _jsonOptions = jsonOptions;
    }

    public async Task<IReadOnlyList<FollowUpQuestion>> GenerateAsync(
        string transcript,
        IReadOnlyList<ConcernItem> concerns,
        CancellationToken cancellationToken)
    {
        if (concerns.Count == 0)
        {
            return [];
        }

        var missingSummaries = BuildMissingSummaries(concerns);
        if (missingSummaries.Count == 0)
        {
            return [];
        }

        var missingJson = JsonSerializer.Serialize(missingSummaries, _jsonOptions);
        var prompt = UserPromptTemplate
            .Replace("<<MISSING_FIELDS>>", missingJson)
            .Replace("<<TRANSCRIPT>>", transcript ?? string.Empty);

        var messages = new List<ChatMessage>
        {
            new("system", SystemPrompt),
            new("user", prompt)
        };

        var completion = await _textClient
            .CompleteAsync(messages, cancellationToken)
            .ConfigureAwait(false);

        if (JsonParsingHelper.TryDeserializeJson<FollowUpPayload>(completion, _jsonOptions, out var payload)
            && payload?.Questions is { Count: > 0 })
        {
            var questions = new List<FollowUpQuestion>();
            foreach (var item in payload.Questions)
            {
                if (!TryMapQuestion(item, concerns, out var question))
                {
                    continue;
                }

                questions.Add(question);
                if (questions.Count >= MaxQuestions)
                {
                    break;
                }
            }

            if (questions.Count > 0)
            {
                return questions;
            }
        }

        return CreateFallbackQuestions(concerns, missingSummaries);
    }

    private static List<MissingFieldSummary> BuildMissingSummaries(IReadOnlyList<ConcernItem> concerns)
    {
        var missingSummaries = new List<MissingFieldSummary>();

        for (var i = 0; i < concerns.Count; i++)
        {
            var concern = concerns[i];
            var missingFields = GetMissingFields(concern);
            if (missingFields.Count == 0)
            {
                continue;
            }

            missingSummaries.Add(new MissingFieldSummary(i, concern.Summary, missingFields));
        }

        return missingSummaries;
    }

    private static IReadOnlyList<string> GetMissingFields(ConcernItem concern)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(concern.Severity)
            || concern.Severity.Equals("unknown", StringComparison.OrdinalIgnoreCase))
        {
            missing.Add("severity");
        }

        if (string.IsNullOrWhiteSpace(concern.Onset))
        {
            missing.Add("onset");
        }

        if (string.IsNullOrWhiteSpace(concern.Duration))
        {
            missing.Add("duration");
        }

        if (string.IsNullOrWhiteSpace(concern.Impact))
        {
            missing.Add("impact");
        }

        if (string.IsNullOrWhiteSpace(concern.Context))
        {
            missing.Add("context");
        }

        return missing;
    }

    private static bool TryMapQuestion(
        FollowUpQuestionContract? contract,
        IReadOnlyList<ConcernItem> concerns,
        out FollowUpQuestion question)
    {
        question = default!;
        if (contract is null || contract.ConcernIndex < 0 || contract.ConcernIndex >= concerns.Count)
        {
            return false;
        }

        var normalizedField = NormalizeField(contract.Field);
        if (string.IsNullOrWhiteSpace(normalizedField))
        {
            return false;
        }

        var summary = concerns[contract.ConcernIndex].Summary;
        var questionText = string.IsNullOrWhiteSpace(contract.Question)
            ? BuildFallbackQuestion(normalizedField, summary)
            : contract.Question.Trim();

        question = new FollowUpQuestion(contract.ConcernIndex, summary, normalizedField, questionText);
        return true;
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

    private static IReadOnlyList<FollowUpQuestion> CreateFallbackQuestions(
        IReadOnlyList<ConcernItem> concerns,
        IReadOnlyList<MissingFieldSummary> missingSummaries)
    {
        var questions = new List<FollowUpQuestion>();

        foreach (var summary in missingSummaries)
        {
            foreach (var field in OrderedFields)
            {
                if (!summary.MissingFields.Contains(field, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var questionText = BuildFallbackQuestion(field, summary.Summary);
                questions.Add(new FollowUpQuestion(summary.Index, summary.Summary, field, questionText));

                if (questions.Count >= MaxQuestions)
                {
                    return questions;
                }
            }
        }

        return questions;
    }

    private static string BuildFallbackQuestion(string field, string summary)
    {
        var label = string.IsNullOrWhiteSpace(summary) ? "this concern" : summary;
        return field switch
        {
            "severity" => $"How severe is \"{label}\"?",
            "onset" => $"When did \"{label}\" start?",
            "duration" => $"How long has \"{label}\" been going on?",
            "impact" => $"How is \"{label}\" affecting daily life?",
            "context" => $"Any additional context for \"{label}\"?",
            _ => $"Can you clarify \"{label}\"?"
        };
    }

    private sealed record MissingFieldSummary(
        int Index,
        string Summary,
        IReadOnlyList<string> MissingFields);

    private sealed class FollowUpPayload
    {
        [JsonPropertyName("questions")]
        public List<FollowUpQuestionContract>? Questions { get; init; }
    }

    private sealed class FollowUpQuestionContract
    {
        [JsonPropertyName("concernIndex")]
        public int ConcernIndex { get; init; }

        [JsonPropertyName("field")]
        public string? Field { get; init; }

        [JsonPropertyName("question")]
        public string? Question { get; init; }
    }
}
