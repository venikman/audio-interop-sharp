using AudioSharp.App.Models;
using AudioSharp.App.Options;
using AudioSharp.App.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace AudioSharp.App.Tests.Services;

[TestClass]
public sealed class FhirObservationMapperTests
{
    [TestMethod]
    public void Map_ConcernWithDetails_AddsNotesAndSubject()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new FhirMappingOptions
        {
            ObservationCodeText = "Patient concern"
        });

        var mapper = new FhirObservationMapper(options);
        var concerns = new List<ConcernItem>
        {
            new(
                "Headache",
                "medium",
                "yesterday",
                "2 days",
                "Interrupted sleep",
                "After a long shift",
                "My head hurts")
        };

        var context = new ProcessingContext("Patient/123", "Jane Doe");

        var result = mapper.Map(concerns, context, new DateTimeOffset(2025, 1, 1, 8, 0, 0, TimeSpan.Zero));

        result.Should().HaveCount(1);
        result[0].Subject.Should().NotBeNull();
        result[0].Subject!.Reference.Should().Be("Patient/123");
        result[0].ValueString.Should().Be("Headache");
        result[0].Note.Should().NotBeNull();
        result[0].Note!.Select(note => note.Text).Should().Contain(text => text.Contains("Severity"));
    }
}
