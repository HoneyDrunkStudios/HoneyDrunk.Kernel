using FluentAssertions;
using HoneyDrunk.Kernel.Context;

namespace HoneyDrunk.Kernel.Tests.Context;

public class GridContextTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesContext()
    {
        // Arrange & Act
        var context = new GridContext(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        // Assert
        context.CorrelationId.Should().Be("corr-123");
        context.NodeId.Should().Be("test-node");
        context.StudioId.Should().Be("test-studio");
        context.Environment.Should().Be("test-env");
        context.CausationId.Should().BeNull();
        context.Baggage.Should().BeEmpty();
        context.Cancellation.Should().Be(default(CancellationToken));
        context.CreatedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithOptionalParameters_CreatesContext()
    {
        // Arrange
        var baggage = new Dictionary<string, string> { ["key1"] = "value1" };
        var cts = new CancellationTokenSource();
        var createdAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var context = new GridContext(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            causationId: "cause-456",
            baggage: baggage,
            cancellation: cts.Token,
            createdAtUtc: createdAt);

        // Assert
        context.CausationId.Should().Be("cause-456");
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        context.Cancellation.Should().Be(cts.Token);
        context.CreatedAtUtc.Should().Be(createdAt);
    }

    [Theory]
    [InlineData("", "node", "studio", "env")]
    [InlineData("corr", "", "studio", "env")]
    [InlineData("corr", "node", "", "env")]
    [InlineData("corr", "node", "studio", "")]
    public void Constructor_NullOrWhitespaceParameters_ThrowsArgumentException(
        string correlationId,
        string nodeId,
        string studioId,
        string environment)
    {
        // Act
        var act = () => new GridContext(correlationId, nodeId, studioId, environment);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BeginScope_ReturnsDisposable()
    {
        // Arrange
        var context = new GridContext("corr", "node", "studio", "env");

        // Act
        var scope = context.BeginScope();

        // Assert
        scope.Should().NotBeNull();
        scope.Should().BeAssignableTo<IDisposable>();

        // Should not throw
        scope.Dispose();
    }

    [Fact]
    public void CreateChildContext_CreatesNewContextWithCausation()
    {
        // Arrange
        var parent = new GridContext(
            correlationId: "parent-corr",
            nodeId: "parent-node",
            studioId: "studio",
            environment: "env",
            baggage: new Dictionary<string, string> { ["key"] = "value" });

        // Act
        var child = parent.CreateChildContext();

        // Assert
        child.CorrelationId.Should().NotBe(parent.CorrelationId);
        child.CausationId.Should().Be(parent.CorrelationId);
        child.NodeId.Should().Be(parent.NodeId);
        child.StudioId.Should().Be(parent.StudioId);
        child.Environment.Should().Be(parent.Environment);
        child.Baggage.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }

    [Fact]
    public void CreateChildContext_WithDifferentNodeId_CreatesContextWithNewNodeId()
    {
        // Arrange
        var parent = new GridContext("corr", "parent-node", "studio", "env");

        // Act
        var child = parent.CreateChildContext("child-node");

        // Assert
        child.NodeId.Should().Be("child-node");
        child.CausationId.Should().Be(parent.CorrelationId);
    }

    [Fact]
    public void WithBaggage_AddsNewBaggage()
    {
        // Arrange
        var context = new GridContext("corr", "node", "studio", "env");

        // Act
        var newContext = context.WithBaggage("new-key", "new-value");

        // Assert
        newContext.Baggage.Should().ContainKey("new-key").WhoseValue.Should().Be("new-value");
        newContext.CorrelationId.Should().Be(context.CorrelationId);
        newContext.NodeId.Should().Be(context.NodeId);
    }

    [Fact]
    public void WithBaggage_UpdatesExistingBaggage()
    {
        // Arrange
        var context = new GridContext(
            "corr",
            "node",
            "studio",
            "env",
            baggage: new Dictionary<string, string> { ["key"] = "old-value" });

        // Act
        var newContext = context.WithBaggage("key", "new-value");

        // Assert
        newContext.Baggage.Should().ContainKey("key").WhoseValue.Should().Be("new-value");
        context.Baggage.Should().ContainKey("key").WhoseValue.Should().Be("old-value"); // Original unchanged
    }

    [Theory]
    [InlineData("", "value")]
    [InlineData("key", "")]
    public void WithBaggage_NullOrWhitespaceParameters_ThrowsArgumentException(string key, string value)
    {
        // Arrange
        var context = new GridContext("corr", "node", "studio", "env");

        // Act
        var act = () => context.WithBaggage(key, value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
