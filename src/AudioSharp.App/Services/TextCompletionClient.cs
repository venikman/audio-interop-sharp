using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AudioSharp.App.Models;
using AudioSharp.App.Options;
using Microsoft.Extensions.Options;

namespace AudioSharp.App.Services;

public sealed class TextCompletionClient : ITextCompletionClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenRouterOptions _openRouterOptions;
    private readonly LmStudioOptions _lmStudioOptions;
    private readonly LlmProviderOptions _providerOptions;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IAiUsageTelemetry _telemetry;

    public TextCompletionClient(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenRouterOptions> openRouterOptions,
        IOptions<LmStudioOptions> lmStudioOptions,
        IOptions<LlmProviderOptions> providerOptions,
        JsonSerializerOptions jsonOptions,
        IAiUsageTelemetry telemetry)
    {
        _httpClientFactory = httpClientFactory;
        _openRouterOptions = openRouterOptions.Value;
        _lmStudioOptions = lmStudioOptions.Value;
        _providerOptions = providerOptions.Value;
        _jsonOptions = jsonOptions;
        _telemetry = telemetry;
    }

    public async Task<string> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken)
    {
        if (messages.Count == 0)
        {
            throw new ArgumentException("At least one message is required.", nameof(messages));
        }

        var (client, model, provider) = ResolveClient();
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        var request = new ChatCompletionRequest
        {
            Model = model,
            Temperature = 0.2m,
            Messages = messages.Select(message => new ChatCompletionMessage
            {
                Role = message.Role,
                Content = message.Content
            }).ToList()
        };

        try
        {
            using var payload = JsonContent.Create(request, options: _jsonOptions);
            using var response = await client
                .PostAsync("chat/completions", payload, cancellationToken)
                .ConfigureAwait(false);

            var responseBody = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Text completion failed: {(int)response.StatusCode} {response.ReasonPhrase}. {responseBody}");
            }

            var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody, _jsonOptions);
            var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Text completion returned empty content.");
            }

            success = true;
            return content.Trim();
        }
        finally
        {
            stopwatch.Stop();
            _telemetry.RecordTextCompletion(provider, model, success, stopwatch.Elapsed);
        }
    }

    private (HttpClient Client, string Model, string Provider) ResolveClient()
    {
        if (_providerOptions.TextProvider == TextProviderKind.OpenRouter)
        {
            if (string.IsNullOrWhiteSpace(_openRouterOptions.ApiKey))
            {
                throw new InvalidOperationException("OpenRouter API key is not configured for text completions.");
            }

            return (_httpClientFactory.CreateClient("OpenRouter"), _openRouterOptions.TextModel, "OpenRouter");
        }

        return (_httpClientFactory.CreateClient("LmStudio"), _lmStudioOptions.TextModel, "LmStudio");
    }

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<ChatCompletionMessage> Messages { get; init; } = [];

        [JsonPropertyName("temperature")]
        public decimal? Temperature { get; init; }
    }

    private sealed class ChatCompletionMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice>? Choices { get; init; }
    }

    private sealed class ChatCompletionChoice
    {
        [JsonPropertyName("message")]
        public ChatCompletionMessageContent? Message { get; init; }
    }

    private sealed class ChatCompletionMessageContent
    {
        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }
}
