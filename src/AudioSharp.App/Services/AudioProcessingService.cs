using AudioSharp.App.Models;

namespace AudioSharp.App.Services;

public sealed class AudioProcessingService : IAudioProcessingService
{
    private readonly IAudioTranscriptionService _transcriptionService;
    private readonly IConcernExtractionService _extractionService;
    private readonly IFhirObservationMapper _observationMapper;
    private readonly IFhirBundleBuilder _bundleBuilder;

    public AudioProcessingService(
        IAudioTranscriptionService transcriptionService,
        IConcernExtractionService extractionService,
        IFhirObservationMapper observationMapper,
        IFhirBundleBuilder bundleBuilder)
    {
        _transcriptionService = transcriptionService;
        _extractionService = extractionService;
        _observationMapper = observationMapper;
        _bundleBuilder = bundleBuilder;
    }

    public async Task<ProcessingResult> ProcessAsync(
        AudioInput audioInput,
        ProcessingContext context,
        CancellationToken cancellationToken)
    {
        var transcript = await _transcriptionService
            .TranscribeAsync(audioInput, cancellationToken)
            .ConfigureAwait(false);

        var extraction = await _extractionService
            .ExtractAsync(transcript.Text, cancellationToken)
            .ConfigureAwait(false);

        var observations = _observationMapper.Map(extraction.Concerns, context, DateTimeOffset.UtcNow);
        var bundle = _bundleBuilder.Build(observations);

        return new ProcessingResult(
            extraction.Transcript,
            extraction.Concerns,
            observations,
            bundle);
    }
}
