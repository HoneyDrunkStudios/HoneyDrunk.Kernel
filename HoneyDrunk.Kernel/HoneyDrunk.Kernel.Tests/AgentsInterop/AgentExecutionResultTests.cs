using System.Text.Json;
using FluentAssertions;
using HoneyDrunk.Kernel.AgentsInterop;

namespace HoneyDrunk.Kernel.Tests.AgentsInterop;

public class AgentExecutionResultTests
{
    [Fact]
    public void DefaultConstructor_CreatesInstanceWithDefaultValues()
    {
        var result = new AgentExecutionResult();

        result.AgentId.Should().BeNull();
        result.CorrelationId.Should().BeNull();
        result.Success.Should().BeFalse();
        result.Result.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.StartedAtUtc.Should().Be(default);
        result.CompletedAtUtc.Should().Be(default);
        result.Metadata.Should().BeNull();
    }

    [Fact]
    public void InitProperties_AllPropertiesCanBeSet()
    {
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var completedAt = DateTimeOffset.UtcNow;
        var metadata = new Dictionary<string, JsonElement>
        {
            ["key1"] = JsonDocument.Parse("\"value1\"").RootElement
        };

        var result = new AgentExecutionResult
        {
            AgentId = "agent-123",
            CorrelationId = "corr-456",
            Success = true,
            Result = JsonDocument.Parse("{\"data\":\"test\"}").RootElement,
            ErrorMessage = null,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt,
            Metadata = metadata
        };

        result.AgentId.Should().Be("agent-123");
        result.CorrelationId.Should().Be("corr-456");
        result.Success.Should().BeTrue();
        result.Result.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();
        result.StartedAtUtc.Should().Be(startedAt);
        result.CompletedAtUtc.Should().Be(completedAt);
        result.Metadata.Should().HaveCount(1);
    }

    [Fact]
    public void SuccessfulExecution_SetsCorrectProperties()
    {
        var result = new AgentExecutionResult
        {
            AgentId = "agent-1",
            CorrelationId = "corr-1",
            Success = true,
            Result = JsonDocument.Parse("{\"output\":\"success\"}").RootElement,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Result.Should().NotBeNull();
    }

    [Fact]
    public void FailedExecution_SetsErrorMessage()
    {
        var result = new AgentExecutionResult
        {
            AgentId = "agent-1",
            CorrelationId = "corr-1",
            Success = false,
            ErrorMessage = "Execution failed",
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Execution failed");
        result.Result.Should().BeNull();
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var completedAt = DateTimeOffset.UtcNow;

        var result1 = new AgentExecutionResult
        {
            AgentId = "agent-1",
            CorrelationId = "corr-1",
            Success = true,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt
        };

        var result2 = new AgentExecutionResult
        {
            AgentId = "agent-1",
            CorrelationId = "corr-1",
            Success = true,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt
        };

        result1.Should().Be(result2);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var result1 = new AgentExecutionResult { AgentId = "agent-1" };
        var result2 = new AgentExecutionResult { AgentId = "agent-2" };

        result1.Should().NotBe(result2);
    }

    [Fact]
    public void With_CreatesNewInstanceWithChanges()
    {
        var original = new AgentExecutionResult
        {
            AgentId = "agent-1",
            Success = false
        };

        var modified = original with { Success = true };

        modified.AgentId.Should().Be("agent-1");
        modified.Success.Should().BeTrue();
        original.Success.Should().BeFalse();
    }

    [Fact]
    public void Metadata_CanContainMultipleEntries()
    {
        var metadata = new Dictionary<string, JsonElement>
        {
            ["key1"] = JsonDocument.Parse("\"value1\"").RootElement,
            ["key2"] = JsonDocument.Parse("123").RootElement,
            ["key3"] = JsonDocument.Parse("true").RootElement
        };

        var result = new AgentExecutionResult { Metadata = metadata };

        result.Metadata.Should().HaveCount(3);
    }
}
