using System.Text.Json;
using AudioSharp.App.Models;
using AudioSharp.App.Services;
using FluentAssertions;
using Moq;

namespace AudioSharp.App.Tests.Services;

[TestClass]
public sealed class ConcernExtractionServiceTests
{
    [TestMethod]
    public async Task ExtractAsync_ValidJson_ReturnsConcerns()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        var client = new Mock<ITextCompletionClient>();
        client
            .Setup(x => x.CompleteAsync(It.IsAny<IReadOnlyList<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"concerns\":[{\"summary\":\"chest pain\",\"severity\":\"high\"}]}");

        var service = new ConcernExtractionService(client.Object, jsonOptions);

        var result = await service.ExtractAsync("Patient reports chest pain.", CancellationToken.None);

        result.Concerns.Should().HaveCount(1);
        result.Concerns[0].Summary.Should().Be("chest pain");
        result.Concerns[0].Severity.Should().Be("high");
    }

    [TestMethod]
    public async Task ExtractAsync_InvalidJson_FallsBackToTranscript()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        var client = new Mock<ITextCompletionClient>();
        client
            .Setup(x => x.CompleteAsync(It.IsAny<IReadOnlyList<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("not-json");

        var service = new ConcernExtractionService(client.Object, jsonOptions);

        var result = await service.ExtractAsync("Sore throat for 3 days.", CancellationToken.None);

        result.Concerns.Should().HaveCount(1);
        result.Concerns[0].Summary.Should().Be("Sore throat for 3 days.");
        result.Concerns[0].Severity.Should().Be("unknown");
    }
}
