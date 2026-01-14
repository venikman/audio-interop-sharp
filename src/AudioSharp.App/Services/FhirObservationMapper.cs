using AudioSharp.App.Models;
using AudioSharp.App.Models.Fhir;
using AudioSharp.App.Options;
using Microsoft.Extensions.Options;

namespace AudioSharp.App.Services;

public sealed class FhirObservationMapper : IFhirObservationMapper
{
    private readonly FhirMappingOptions _options;

    public FhirObservationMapper(IOptions<FhirMappingOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<FhirObservation> Map(
        IReadOnlyList<ConcernItem> concerns,
        ProcessingContext context,
        DateTimeOffset recordedAt)
    {
        var observations = new List<FhirObservation>();
        foreach (var concern in concerns)
        {
            var notes = BuildNotes(concern);
            var observation = new FhirObservation
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = "final",
                Code = BuildCode(),
                Subject = BuildSubject(context),
                EffectiveDateTime = recordedAt.ToString("O"),
                ValueString = concern.Summary,
                Note = notes.Count > 0 ? notes : null
            };

            observations.Add(observation);
        }

        return observations;
    }

    private FhirCodeableConcept BuildCode()
    {
        List<FhirCoding>? coding = null;

        if (!string.IsNullOrWhiteSpace(_options.ObservationCodeSystem)
            && !string.IsNullOrWhiteSpace(_options.ObservationCode))
        {
            coding =
            [
                new FhirCoding
                {
                    System = _options.ObservationCodeSystem,
                    Code = _options.ObservationCode,
                    Display = _options.ObservationCodeDisplay
                }
            ];
        }

        return new FhirCodeableConcept
        {
            Text = _options.ObservationCodeDisplay ?? _options.ObservationCodeText,
            Coding = coding
        };
    }

    private static FhirReference? BuildSubject(ProcessingContext context)
    {
        if (string.IsNullOrWhiteSpace(context.SubjectReference)
            && string.IsNullOrWhiteSpace(context.SubjectDisplay))
        {
            return null;
        }

        return new FhirReference
        {
            Reference = context.SubjectReference,
            Display = context.SubjectDisplay
        };
    }

    private static List<FhirAnnotation> BuildNotes(ConcernItem concern)
    {
        var notes = new List<FhirAnnotation>();

        if (!string.IsNullOrWhiteSpace(concern.Severity))
        {
            notes.Add(new FhirAnnotation($"Severity: {concern.Severity}"));
        }

        if (!string.IsNullOrWhiteSpace(concern.Onset))
        {
            notes.Add(new FhirAnnotation($"Onset: {concern.Onset}"));
        }

        if (!string.IsNullOrWhiteSpace(concern.Duration))
        {
            notes.Add(new FhirAnnotation($"Duration: {concern.Duration}"));
        }

        if (!string.IsNullOrWhiteSpace(concern.Impact))
        {
            notes.Add(new FhirAnnotation($"Impact: {concern.Impact}"));
        }

        if (!string.IsNullOrWhiteSpace(concern.Context))
        {
            notes.Add(new FhirAnnotation($"Context: {concern.Context}"));
        }

        if (!string.IsNullOrWhiteSpace(concern.PatientQuote))
        {
            notes.Add(new FhirAnnotation($"Quote: \"{concern.PatientQuote}\""));
        }

        return notes;
    }
}
