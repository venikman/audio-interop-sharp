using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AudioSharp.App.Models;
using AudioSharp.App.Options;
using Microsoft.Extensions.Options;

namespace AudioSharp.App.Services;

public sealed class OpenRouterAudioTranscriptionService : IAudioTranscriptionService
{
    private const string TranscriptionPrompt =
        "Transcribe the audio to plain text. Preserve meaning and keep it concise.";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenRouterOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IAiUsageTelemetry _telemetry;

    public OpenRouterAudioTranscriptionService(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenRouterOptions> options,
        JsonSerializerOptions jsonOptions,
        IAiUsageTelemetry telemetry)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _jsonOptions = jsonOptions;
        _telemetry = telemetry;
    }

    public async Task<TranscriptResult> TranscribeAsync(AudioInput audioInput, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenRouter API key is not configured.");
        }

        var client = _httpClientFactory.CreateClient("OpenRouter");
        var format = AudioFormatHelper.ResolveFormat(audioInput.ContentType);
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var request = new OpenRouterChatRequest
            {
                Model = _options.AudioModel,
                Temperature = 0.2m,
                Messages =
                [
                    new OpenRouterMessage
                    {
                        Role = "user",
                        Content =
                        [
                            new OpenRouterContentPart
                            {
                                Type = "input_audio",
                                InputAudio = new OpenRouterInputAudio
                                {
                                    Data = audioInput.Base64Data,
                                    Format = format
                                }
                            },
                            new OpenRouterContentPart
                            {
                                Type = "text",
                                Text = TranscriptionPrompt
                            }
                        ]
                    }
                ]
            };

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
                    $"OpenRouter transcription failed: {(int)response.StatusCode} {response.ReasonPhrase}. {TrimForLog(responseBody)}");
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType is not null && !mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"OpenRouter transcription returned non-JSON content ({mediaType}). {TrimForLog(responseBody)}");
            }

            if (TryGetError(responseBody, out var errorMessage))
            {
                throw new InvalidOperationException($"OpenRouter transcription error: {errorMessage}");
            }

            var completion = JsonSerializer.Deserialize<OpenRouterChatResponse>(responseBody, _jsonOptions);
            var transcript = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
            if (string.IsNullOrWhiteSpace(transcript))
            {
                throw new InvalidOperationException("OpenRouter transcription returned empty content.");
            }

            success = true;
            return new TranscriptResult(transcript);
        }
        finally
        {
            stopwatch.Stop();
            _telemetry.RecordAudioTranscription("OpenRouter", _options.AudioModel, success, stopwatch.Elapsed);
        }
    }

    private sealed class OpenRouterChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenRouterMessage> Messages { get; init; } = [];

        [JsonPropertyName("temperature")]
        public decimal? Temperature { get; init; }
    }

    private sealed class OpenRouterMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public List<OpenRouterContentPart> Content { get; init; } = [];
    }

    private sealed class OpenRouterContentPart
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;

        [JsonPropertyName("text")]
        public string? Text { get; init; }

        [JsonPropertyName("input_audio")]
        public OpenRouterInputAudio? InputAudio { get; init; }
    }

    private sealed class OpenRouterInputAudio
    {
        [JsonPropertyName("data")]
        public string Data { get; init; } = string.Empty;

        [JsonPropertyName("format")]
        public string Format { get; init; } = "wav";
    }

    private sealed class OpenRouterChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenRouterChoice>? Choices { get; init; }
    }

    private sealed class OpenRouterChoice
    {
        [JsonPropertyName("message")]
        public OpenRouterResponseMessage? Message { get; init; }
    }

    private sealed class OpenRouterResponseMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }

    private static bool TryGetError(string responseBody, out string? message)
    {
        message = null;
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return false;
        }

        try
        {
            var error = JsonSerializer.Deserialize<OpenRouterErrorEnvelope>(responseBody);
            message = error?.Error?.Message;
            return !string.IsNullOrWhiteSpace(message);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string TrimForLog(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= 800 ? value : $"{value[..800]}...";
    }

    private sealed class OpenRouterErrorEnvelope
    {
        [JsonPropertyName("error")]
        public OpenRouterError? Error { get; init; }
    }

    private sealed class OpenRouterError
    {
        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }
}
