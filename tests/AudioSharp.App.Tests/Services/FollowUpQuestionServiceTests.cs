using System.Text.Json;
using AudioSharp.App.Models;
using AudioSharp.App.Services;
using FluentAssertions;
using Moq;

namespace AudioSharp.App.Tests.Services;

[TestClass]
public sealed class FollowUpQuestionServiceTests
{
    [TestMethod]
    public async Task GenerateAsync_ValidJson_ReturnsQuestions()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        var client = new Mock<ITextCompletionClient>();
        client
            .Setup(x => x.CompleteAsync(It.IsAny<IReadOnlyList<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"questions\":[{\"concernIndex\":0,\"field\":\"severity\",\"question\":\"How severe is the headache?\"}]}");

        var service = new FollowUpQuestionService(client.Object, jsonOptions);
        var concerns = new List<ConcernItem>
        {
            new("headache", null, null, null, null, null, null)
        };

        var result = await service.GenerateAsync("Patient reports headache.", concerns, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].ConcernIndex.Should().Be(0);
        result[0].Field.Should().Be("severity");
        result[0].Question.Should().Contain("headache");
    }

    [TestMethod]
    public async Task GenerateAsync_InvalidJson_UsesFallbackQuestions()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        var client = new Mock<ITextCompletionClient>();
        client
            .Setup(x => x.CompleteAsync(It.IsAny<IReadOnlyList<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("not-json");

        var service = new FollowUpQuestionService(client.Object, jsonOptions);
        var concerns = new List<ConcernItem>
        {
            new("sore throat", "unknown", null, null, null, null, null)
        };

        var result = await service.GenerateAsync("Sore throat.", concerns, CancellationToken.None);

        result.Should().NotBeEmpty();
        result[0].Field.Should().Be("severity");
    }
}
