using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Agents;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.AgentsInterop;
using HoneyDrunk.Kernel.Context;

namespace HoneyDrunk.Kernel.Tests.AgentsInterop;

public class AgentContextProjectionTests
{
    [Fact]
    public void ProjectToAgentContext_ValidInputs_CreatesContext()
    {
        // Arrange
        var gridContext = CreateTestGridContext();
        var operationContext = CreateTestOperationContext(gridContext);
        var agentDescriptor = CreateTestAgentDescriptor();

        // Act
        var agentContext = AgentContextProjection.ProjectToAgentContext(
            gridContext,
            operationContext,
            agentDescriptor);

        // Assert
        agentContext.Should().NotBeNull();
        agentContext.GridContext.Should().Be(gridContext);
        agentContext.OperationContext.Should().Be(operationContext);
        agentContext.Agent.Should().Be(agentDescriptor);
        agentContext.StartedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        agentContext.ExecutionMetadata.Should().BeEmpty();
    }

    [Fact]
    public void ProjectToAgentContext_WithMetadata_IncludesMetadata()
    {
        // Arrange
        var gridContext = CreateTestGridContext();
        var operationContext = CreateTestOperationContext(gridContext);
        var agentDescriptor = CreateTestAgentDescriptor();
        var metadata = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        // Act
        var agentContext = AgentContextProjection.ProjectToAgentContext(
            gridContext,
            operationContext,
            agentDescriptor,
            metadata);

        // Assert
        agentContext.ExecutionMetadata.Should().HaveCount(2);
        agentContext.ExecutionMetadata["key1"].Should().Be("value1");
        agentContext.ExecutionMetadata["key2"].Should().Be(42);
    }

    [Fact]
    public void AddMetadata_AddsNewMetadata()
    {
        // Arrange
        var agentContext = AgentContextProjection.ProjectToAgentContext(
            CreateTestGridContext(),
            CreateTestOperationContext(CreateTestGridContext()),
            CreateTestAgentDescriptor());

        // Act
        agentContext.AddMetadata("tool_calls", 5);
        agentContext.AddMetadata("tokens_used", 1000);

        // Assert
        agentContext.ExecutionMetadata.Should().HaveCount(2);
        agentContext.ExecutionMetadata["tool_calls"].Should().Be(5);
        agentContext.ExecutionMetadata["tokens_used"].Should().Be(1000);
    }

    [Fact]
    public void AddMetadata_UpdatesExistingMetadata()
    {
        // Arrange
        var agentContext = AgentContextProjection.ProjectToAgentContext(
            CreateTestGridContext(),
            CreateTestOperationContext(CreateTestGridContext()),
            CreateTestAgentDescriptor(),
            new Dictionary<string, object?> { ["counter"] = 1 });

        // Act
        agentContext.AddMetadata("counter", 2);

        // Assert
        agentContext.ExecutionMetadata["counter"].Should().Be(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void AddMetadata_InvalidKey_ThrowsArgumentException(string? key)
    {
        // Arrange
        var agentContext = AgentContextProjection.ProjectToAgentContext(
            CreateTestGridContext(),
            CreateTestOperationContext(CreateTestGridContext()),
            CreateTestAgentDescriptor());

        // Act
        var act = () => agentContext.AddMetadata(key!, "value");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CanAccess_AgentHasCapability_ReturnsTrue()
    {
        // Arrange
        var agentDescriptor = new TestAgentDescriptor
        {
            AgentId = "test-agent",
            Capabilities =
            [
                new TestAgentCapability("access:data")
            ]
        };
        var agentContext = AgentContextProjection.ProjectToAgentContext(
            CreateTestGridContext(),
            CreateTestOperationContext(CreateTestGridContext()),
            agentDescriptor);

        // Act
        var canAccess = agentContext.CanAccess("data", "resource-123");

        // Assert
        canAccess.Should().BeTrue();
    }

    [Fact]
    public void CanAccess_AgentLacksCapability_ReturnsFalse()
    {
        // Arrange
        var agentDescriptor = new TestAgentDescriptor
        {
            AgentId = "test-agent",
            Capabilities = []
        };
        var agentContext = AgentContextProjection.ProjectToAgentContext(
            CreateTestGridContext(),
            CreateTestOperationContext(CreateTestGridContext()),
            agentDescriptor);

        // Act
        var canAccess = agentContext.CanAccess("secrets", "api-key");

        // Assert
        canAccess.Should().BeFalse();
    }

    private static GridContext CreateTestGridContext()
    {
        return new GridContext(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test");
    }

    private static OperationContext CreateTestOperationContext(IGridContext gridContext)
    {
        return new OperationContext(
            gridContext,
            "TestOperation",
            Ulid.NewUlid().ToString());
    }

    private static TestAgentDescriptor CreateTestAgentDescriptor()
    {
        return new TestAgentDescriptor
        {
            AgentId = "test-agent",
            Capabilities = []
        };
    }

    private sealed class TestAgentDescriptor : IAgentDescriptor
    {
        public string AgentId { get; init; } = string.Empty;

        public string Name { get; init; } = "Test Agent";

        public string Description { get; init; } = "Test agent for unit testing";

        public string Version { get; init; } = "1.0.0";

        public string AgentType { get; init; } = "test";

        public AgentContextScope ContextScope { get; init; } = AgentContextScope.Standard;

        public IReadOnlyList<IAgentCapability> Capabilities { get; init; } = [];

        public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

        public bool HasCapability(string capabilityName) =>
            Capabilities.Any(c => c.Name == capabilityName);
    }

    private sealed class TestAgentCapability(string name) : IAgentCapability
    {
        public string Name { get; } = name;

        public string Description { get; } = $"Test capability: {name}";

        public string Version { get; } = "1.0.0";

        public string Category { get; } = "test";

        public string PermissionLevel { get; } = "read";

        public IReadOnlyList<string> RequiredPermissions { get; } = [];

        public IReadOnlyDictionary<string, string> Constraints { get; } = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();

        public bool ValidateParameters(IReadOnlyDictionary<string, object?> parameters, out string? errorMessage)
        {
            errorMessage = null;
            return true;
        }
    }
}
