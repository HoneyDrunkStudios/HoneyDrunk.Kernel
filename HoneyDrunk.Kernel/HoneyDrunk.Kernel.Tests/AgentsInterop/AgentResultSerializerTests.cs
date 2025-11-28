using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Agents;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.AgentsInterop;
using HoneyDrunk.Kernel.Context;

namespace HoneyDrunk.Kernel.Tests.AgentsInterop;

public class AgentResultSerializerTests
{
    [Fact]
    public void SerializeResult_NullContext_ThrowsArgumentNullException()
    {
        var act = () => AgentResultSerializer.SerializeResult(null!, true);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SerializeResult_ValidContext_ReturnsJsonString()
    {
        var context = CreateTestAgentExecutionContext();

        var json = AgentResultSerializer.SerializeResult(context, success: true);

        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("agentId");
        json.Should().Contain("correlationId");
    }

    [Fact]
    public void SerializeResult_Success_SerializesCorrectly()
    {
        var context = CreateTestAgentExecutionContext();
        var result = new { data = "test-data" };

        var json = AgentResultSerializer.SerializeResult(context, success: true, result: result);

        json.Should().Contain("\"success\":true");
        json.Should().Contain("test-data");
    }

    [Fact]
    public void SerializeResult_Failure_IncludesErrorMessage()
    {
        var context = CreateTestAgentExecutionContext();
        var errorMessage = "Test error occurred";

        var json = AgentResultSerializer.SerializeResult(
            context,
            success: false,
            errorMessage: errorMessage);

        json.Should().Contain("\"success\":false");
        json.Should().Contain(errorMessage);
    }

    [Fact]
    public void SerializeResult_IncludesAgentIdAndCorrelationId()
    {
        var context = CreateTestAgentExecutionContext();

        var json = AgentResultSerializer.SerializeResult(context, success: true);

        json.Should().Contain("\"agentId\":\"test-agent\"");
        json.Should().Contain("\"correlationId\":\"corr-123\"");
    }

    [Fact]
    public void SerializeResult_IncludesTimestamps()
    {
        var context = CreateTestAgentExecutionContext();

        var json = AgentResultSerializer.SerializeResult(context, success: true);

        json.Should().Contain("startedAtUtc");
        json.Should().Contain("completedAtUtc");
    }

    [Fact]
    public void SerializeResult_IncludesMetadata()
    {
        var context = CreateTestAgentExecutionContext();

        var json = AgentResultSerializer.SerializeResult(context, success: true);

        json.Should().Contain("metadata");
    }

    [Fact]
    public void SerializeResult_UsesCamelCase()
    {
        var context = CreateTestAgentExecutionContext();

        var json = AgentResultSerializer.SerializeResult(context, success: true);

        json.Should().Contain("agentId");
        json.Should().NotContain("AgentId");
        json.Should().Contain("correlationId");
        json.Should().NotContain("CorrelationId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void DeserializeResult_NullOrWhitespaceJson_ThrowsArgumentException(string? json)
    {
        var act = () => AgentResultSerializer.DeserializeResult(json!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DeserializeResult_ValidJson_ReturnsAgentExecutionResult()
    {
        var json = @"{
            ""agentId"": ""agent-123"",
            ""correlationId"": ""corr-456"",
            ""success"": true,
            ""result"": { ""data"": ""test"" },
            ""startedAtUtc"": ""2025-01-01T00:00:00Z"",
            ""completedAtUtc"": ""2025-01-01T00:01:00Z""
        }";

        var result = AgentResultSerializer.DeserializeResult(json);

        result.Should().NotBeNull();
        result!.AgentId.Should().Be("agent-123");
        result.CorrelationId.Should().Be("corr-456");
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void DeserializeResult_SuccessfulResult_HasNoErrorMessage()
    {
        var json = @"{
            ""agentId"": ""agent-1"",
            ""correlationId"": ""corr-1"",
            ""success"": true,
            ""result"": { ""output"": ""success"" },
            ""startedAtUtc"": ""2025-01-01T00:00:00Z"",
            ""completedAtUtc"": ""2025-01-01T00:01:00Z""
        }";

        var result = AgentResultSerializer.DeserializeResult(json);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void DeserializeResult_FailedResult_HasErrorMessage()
    {
        var json = @"{
            ""agentId"": ""agent-1"",
            ""correlationId"": ""corr-1"",
            ""success"": false,
            ""errorMessage"": ""Execution failed"",
            ""startedAtUtc"": ""2025-01-01T00:00:00Z"",
            ""completedAtUtc"": ""2025-01-01T00:01:00Z""
        }";

        var result = AgentResultSerializer.DeserializeResult(json);

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Execution failed");
    }

    [Fact]
    public void DeserializeResult_WithMetadata_DeserializesMetadata()
    {
        var json = @"{
            ""agentId"": ""agent-1"",
            ""correlationId"": ""corr-1"",
            ""success"": true,
            ""startedAtUtc"": ""2025-01-01T00:00:00Z"",
            ""completedAtUtc"": ""2025-01-01T00:01:00Z"",
            ""metadata"": {
                ""key1"": ""value1"",
                ""key2"": 123
            }
        }";

        var result = AgentResultSerializer.DeserializeResult(json);

        result.Should().NotBeNull();
        result!.Metadata.Should().NotBeNull();
        result.Metadata.Should().HaveCount(2);
    }

    [Fact]
    public void DeserializeResult_InvalidJson_ReturnsNull()
    {
        var json = "{ invalid json }";

        var result = AgentResultSerializer.DeserializeResult(json);

        result.Should().BeNull();
    }

    [Fact]
    public void DeserializeResult_EmptyObject_ReturnsResult()
    {
        var json = "{}";

        var result = AgentResultSerializer.DeserializeResult(json);

        result.Should().NotBeNull();
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        var context = CreateTestAgentExecutionContext();
        var originalResult = new { data = "test", value = 42 };

        var json = AgentResultSerializer.SerializeResult(context, success: true, result: originalResult);
        var deserialized = AgentResultSerializer.DeserializeResult(json);

        deserialized.Should().NotBeNull();
        deserialized!.AgentId.Should().Be("test-agent");
        deserialized.CorrelationId.Should().Be("corr-123");
        deserialized.Success.Should().BeTrue();
    }

    [Fact]
    public void SerializeResult_NullResult_SerializesNullResult()
    {
        var context = CreateTestAgentExecutionContext();

        var json = AgentResultSerializer.SerializeResult(context, success: true, result: null);

        json.Should().Contain("\"result\":null");
    }

    [Fact]
    public void SerializeResult_NullErrorMessage_SerializesNullError()
    {
        var context = CreateTestAgentExecutionContext();

        var json = AgentResultSerializer.SerializeResult(context, success: false, errorMessage: null);

        json.Should().Contain("\"errorMessage\":null");
    }

    [Fact]
    public void SerializeResult_ComplexNestedObject_SerializesCorrectly()
    {
        var context = CreateTestAgentExecutionContext();
        var complexResult = new
        {
            user = new { id = "user-123", name = "Test User" },
            items = new[] { 1, 2, 3 },
            metadata = new Dictionary<string, object> { ["key"] = "value" }
        };

        var json = AgentResultSerializer.SerializeResult(context, success: true, result: complexResult);
        var deserialized = AgentResultSerializer.DeserializeResult(json);

        deserialized.Should().NotBeNull();
        deserialized!.Success.Should().BeTrue();
    }

    [Fact]
    public void DeserializeResult_MissingRequiredField_ReturnsPartialResult()
    {
        var json = @"{
            ""agentId"": ""agent-123"",
            ""success"": true
        }";

        var result = AgentResultSerializer.DeserializeResult(json);

        result.Should().NotBeNull();
        result!.AgentId.Should().Be("agent-123");
    }

    [Fact]
    public void DeserializeResult_WithExtraFields_IgnoresExtraFields()
    {
        var json = @"{
            ""agentId"": ""agent-123"",
            ""correlationId"": ""corr-456"",
            ""success"": true,
            ""extraField"": ""should-be-ignored"",
            ""anotherExtra"": 12345,
            ""startedAtUtc"": ""2025-01-01T00:00:00Z"",
            ""completedAtUtc"": ""2025-01-01T00:01:00Z""
        }";

        var result = AgentResultSerializer.DeserializeResult(json);

        result.Should().NotBeNull();
        result!.AgentId.Should().Be("agent-123");
    }

    [Fact]
    public void SerializeResult_EmptyErrorMessage_SerializesEmpty()
    {
        var context = CreateTestAgentExecutionContext();

        var json = AgentResultSerializer.SerializeResult(context, success: false, errorMessage: string.Empty);

        json.Should().Contain("\"errorMessage\":\"\"");
    }

    [Fact]
    public void SerializeResult_WithSpecialCharacters_EscapesCorrectly()
    {
        var context = CreateTestAgentExecutionContext();
        var resultWithSpecialChars = new
        {
            message = "Error with \"quotes\" and \nnewlines",
            path = "C:\\Users\\test\\file.txt"
        };

        var json = AgentResultSerializer.SerializeResult(context, success: true, result: resultWithSpecialChars);
        var deserialized = AgentResultSerializer.DeserializeResult(json);

        deserialized.Should().NotBeNull();
        deserialized!.Success.Should().BeTrue();
    }

    [Fact]
    public void SerializeResult_LargeResult_HandlesCorrectly()
    {
        var context = CreateTestAgentExecutionContext();
        var largeResult = new
        {
            items = Enumerable.Range(0, 1000).Select(i => new { id = i, name = $"Item-{i}" })
        };

        var json = AgentResultSerializer.SerializeResult(context, success: true, result: largeResult);

        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("Item-999");
    }

    private static TestAgentExecutionContext CreateTestAgentExecutionContext()
    {
        return new TestAgentExecutionContext
        {
            Agent = new TestAgentDescriptor { AgentId = "test-agent" },
            GridContext = new GridContext("corr-123", "test-node", "test-studio", "test"),
            StartedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private sealed class TestAgentExecutionContext : IAgentExecutionContext
    {
        public required IAgentDescriptor Agent { get; init; }

        public required IGridContext GridContext { get; init; }

        public IOperationContext OperationContext => null!;

        public DateTimeOffset StartedAtUtc { get; init; }

        public IReadOnlyDictionary<string, object?> ExecutionMetadata { get; } = new Dictionary<string, object?>();

        public void AddMetadata(string key, object? value)
        {
        }

        public bool CanAccess(string resourceType, string resourceId) => true;
    }

    private sealed class TestAgentDescriptor : IAgentDescriptor
    {
        public required string AgentId { get; init; }

        public string Name => "Test Agent";

        public string AgentType => "test";

        public string Version => "1.0.0";

        public IReadOnlyList<IAgentCapability> Capabilities => [];

        public AgentContextScope ContextScope => AgentContextScope.Full;

        public IReadOnlyDictionary<string, string> Metadata => new Dictionary<string, string>();

        public bool HasCapability(string capabilityName) => false;
    }
}
