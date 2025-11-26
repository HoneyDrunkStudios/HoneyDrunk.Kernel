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
            operationId: Ulid.NewUlid().ToString(),
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
        using var cts = new CancellationTokenSource();
        var createdAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var context = new GridContext(
            correlationId: "corr-123",
            operationId: Ulid.NewUlid().ToString(),
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

    [Fact]
    public void Constructor_WithMultipleBaggageItems_CreatesContextWithAllItems()
    {
        // Arrange
        var baggage = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
            ["key3"] = "value3"
        };

        // Act
        var context = new GridContext(
            correlationId: "corr-123",
            operationId: Ulid.NewUlid().ToString(),
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            baggage: baggage);

        // Assert
        context.Baggage.Should().HaveCount(3);
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        context.Baggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
        context.Baggage.Should().ContainKey("key3").WhoseValue.Should().Be("value3");
    }

    [Fact]
    public void Constructor_WithNullBaggage_CreatesContextWithEmptyBaggage()
    {
        // Act
        var context = new GridContext(
            correlationId: "corr-123",
            operationId: Ulid.NewUlid().ToString(),
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            baggage: null);

        // Assert
        context.Baggage.Should().NotBeNull();
        context.Baggage.Should().BeEmpty();
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
        var act = () => new GridContext(correlationId, Ulid.NewUlid().ToString(), nodeId, studioId, environment);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null, "node", "studio", "env")]
    [InlineData("corr", null, "studio", "env")]
    [InlineData("corr", "node", null, "env")]
    [InlineData("corr", "node", "studio", null)]
    public void Constructor_NullParameters_ThrowsArgumentException(
        string? correlationId,
        string? nodeId,
        string? studioId,
        string? environment)
    {
        // Act
        var act = () => new GridContext(correlationId!, Ulid.NewUlid().ToString(), nodeId!, studioId!, environment!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BeginScope_ReturnsDisposable()
    {
        // Arrange
        var context = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");

        // Act
        var scope = context.BeginScope();

        // Assert
        scope.Should().NotBeNull();
        scope.Should().BeAssignableTo<IDisposable>();

        // Should not throw
        scope.Dispose();
    }

    [Fact]
    public void BeginScope_CanBeCalledMultipleTimes()
    {
        // Arrange
        var context = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");

        // Act
        var scope1 = context.BeginScope();
        var scope2 = context.BeginScope();
        var scope3 = context.BeginScope();

        // Assert
        scope1.Should().NotBeNull();
        scope2.Should().NotBeNull();
        scope3.Should().NotBeNull();

        // Should not throw
        scope1.Dispose();
        scope2.Dispose();
        scope3.Dispose();
    }

    [Fact]
    public void BeginScope_DisposeCanBeCalledMultipleTimes()
    {
        // Arrange
        var context = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
        var scope = context.BeginScope();

        // Act & Assert - should not throw
        scope.Dispose();
        scope.Dispose();
        scope.Dispose();
    }

    [Fact]
    public void CreateChildContext_CreatesNewContextWithCausation()
    {
        // Arrange
        var parent = new GridContext(
            correlationId: "parent-corr",
            operationId: Ulid.NewUlid().ToString(),
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
        var parent = new GridContext("corr", Ulid.NewUlid().ToString(), "parent-node", "studio", "env");

        // Act
        var child = parent.CreateChildContext("child-node");

        // Assert
        child.NodeId.Should().Be("child-node");
        child.CausationId.Should().Be(parent.CorrelationId);
    }

    [Fact]
    public void CreateChildContext_PropagatesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var parent = new GridContext(
            correlationId: "corr",
            operationId: Ulid.NewUlid().ToString(),
            nodeId: "node",
            studioId: "studio",
            environment: "env",
            cancellation: cts.Token);

        // Act
        var child = parent.CreateChildContext();

        // Assert
        child.Cancellation.Should().Be(cts.Token);
    }

    [Fact]
    public void CreateChildContext_WhenParentHasCausationId_ChildCausationIsParentCorrelation()
    {
        // Arrange
        var grandparent = new GridContext("grandparent-corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
        var parent = grandparent.CreateChildContext();

        // Act
        var child = parent.CreateChildContext();

        // Assert
        parent.CausationId.Should().Be("grandparent-corr");
        child.CausationId.Should().Be(parent.CorrelationId);
        child.CausationId.Should().NotBe(grandparent.CorrelationId);
    }

    [Fact]
    public void CreateChildContext_PreservesAllBaggageItems()
    {
        // Arrange
        var parent = new GridContext(
            correlationId: "corr",
            operationId: Ulid.NewUlid().ToString(),
            nodeId: "node",
            studioId: "studio",
            environment: "env",
            baggage: new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2",
                ["key3"] = "value3"
            });

        // Act
        var child = parent.CreateChildContext();

        // Assert
        child.Baggage.Should().HaveCount(3);
        child.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        child.Baggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
        child.Baggage.Should().ContainKey("key3").WhoseValue.Should().Be("value3");
    }

    [Fact]
    public void CreateChildContext_CreatesNewUlidForCorrelationId()
    {
        // Arrange
        var parent = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");

        // Act
        var child1 = parent.CreateChildContext();
        var child2 = parent.CreateChildContext();

        // Assert
        child1.CorrelationId.Should().NotBe(child2.CorrelationId);
        child1.CorrelationId.Should().NotBeNullOrWhiteSpace();
        child2.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void WithBaggage_AddsNewBaggage()
    {
        // Arrange
        var context = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");

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
            Ulid.NewUlid().ToString(),
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

    [Fact]
    public void WithBaggage_PreservesAllContextProperties()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var createdAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var context = new GridContext(
            correlationId: "corr-123",
            operationId: Ulid.NewUlid().ToString(),
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            causationId: "cause-456",
            baggage: new Dictionary<string, string> { ["existing"] = "value" },
            cancellation: cts.Token,
            createdAtUtc: createdAt);

        // Act
        var newContext = context.WithBaggage("new-key", "new-value");

        // Assert
        newContext.CorrelationId.Should().Be("corr-123");
        newContext.NodeId.Should().Be("test-node");
        newContext.StudioId.Should().Be("test-studio");
        newContext.Environment.Should().Be("test-env");
        newContext.CausationId.Should().Be("cause-456");
        newContext.Cancellation.Should().Be(cts.Token);
        newContext.CreatedAtUtc.Should().Be(createdAt);
        newContext.Baggage.Should().HaveCount(2);
        newContext.Baggage.Should().ContainKey("existing").WhoseValue.Should().Be("value");
        newContext.Baggage.Should().ContainKey("new-key").WhoseValue.Should().Be("new-value");
    }

    [Fact]
    public void WithBaggage_PreservesExistingBaggageItems()
    {
        // Arrange
        var context = new GridContext(
            "corr",
            Ulid.NewUlid().ToString(),
            "node",
            "studio",
            "env",
            baggage: new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            });

        // Act
        var newContext = context.WithBaggage("key3", "value3");

        // Assert
        newContext.Baggage.Should().HaveCount(3);
        newContext.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        newContext.Baggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
        newContext.Baggage.Should().ContainKey("key3").WhoseValue.Should().Be("value3");
    }

    [Fact]
    public void WithBaggage_DoesNotModifyOriginalContext()
    {
        // Arrange
        var originalBaggage = new Dictionary<string, string> { ["key1"] = "value1" };
        var context = new GridContext(
            "corr",
            Ulid.NewUlid().ToString(),
            "node",
            "studio",
            "env",
            baggage: originalBaggage);

        // Act
        var newContext = context.WithBaggage("key2", "value2");

        // Assert
        context.Baggage.Should().HaveCount(1);
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        context.Baggage.Should().NotContainKey("key2");
        newContext.Baggage.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("", "value")]
    [InlineData("key", "")]
    public void WithBaggage_NullOrWhitespaceParameters_ThrowsArgumentException(string key, string value)
    {
        // Arrange
        var context = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");

        // Act
        var act = () => context.WithBaggage(key, value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null, "value")]
    [InlineData("key", null)]
    public void WithBaggage_NullParameters_ThrowsArgumentException(string? key, string? value)
    {
        // Arrange
        var context = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");

        // Act
        var act = () => context.WithBaggage(key!, value!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Baggage_IsImmutable()
    {
        // Arrange
        var baggageDict = new Dictionary<string, string> { ["key"] = "value" };
        var context = new GridContext(
            "corr",
            Ulid.NewUlid().ToString(),
            "node",
            "studio",
            "env",
            baggage: baggageDict);

        // Act - Modify the original dictionary
        baggageDict["key"] = "modified";
        baggageDict["new-key"] = "new-value";

        // Assert - Context baggage should be unchanged
        context.Baggage.Should().HaveCount(1);
        context.Baggage.Should().ContainKey("key").WhoseValue.Should().Be("value");
        context.Baggage.Should().NotContainKey("new-key");
    }
}
