using System.Text.Json;
using AudioSharp.App.Models;
using AudioSharp.App.Services;
using FluentAssertions;
using Moq;

namespace AudioSharp.App.Tests.Services;

[TestClass]
public sealed class ConcernRefinementServiceTests
{
    [TestMethod]
    public async Task ApplyAnswersAsync_ValidJson_ReturnsUpdatedConcerns()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        var client = new Mock<ITextCompletionClient>();
        client
            .Setup(x => x.CompleteAsync(It.IsAny<IReadOnlyList<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"concerns\":[{\"summary\":\"headache\",\"severity\":\"high\"}]}");

        var service = new ConcernRefinementService(client.Object, jsonOptions);
        var concerns = new List<ConcernItem>
        {
            new("headache", "unknown", null, null, null, null, null)
        };
        var answers = new List<FollowUpAnswer>
        {
            new(0, "severity", "high")
        };

        var result = await service.ApplyAnswersAsync("Patient reports headache.", concerns, answers, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Severity.Should().Be("high");
    }

    [TestMethod]
    public async Task ApplyAnswersAsync_InvalidJson_FallsBackToAnswers()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        var client = new Mock<ITextCompletionClient>();
        client
            .Setup(x => x.CompleteAsync(It.IsAny<IReadOnlyList<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("not-json");

        var service = new ConcernRefinementService(client.Object, jsonOptions);
        var concerns = new List<ConcernItem>
        {
            new("fatigue", null, null, null, null, null, null)
        };
        var answers = new List<FollowUpAnswer>
        {
            new(0, "Impact", "Hard to work")
        };

        var result = await service.ApplyAnswersAsync("Fatigue reported.", concerns, answers, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Impact.Should().Be("Hard to work");
    }
}
