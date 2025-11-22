using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Context;

namespace HoneyDrunk.Kernel.Tests.Context;

public class NodeContextTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesContext()
    {
        // Arrange & Act
        var context = new NodeContext(
            nodeId: "test-node",
            version: "1.0.0",
            studioId: "test-studio",
            environment: "production");

        // Assert
        context.NodeId.Should().Be("test-node");
        context.Version.Should().Be("1.0.0");
        context.StudioId.Should().Be("test-studio");
        context.Environment.Should().Be("production");
        context.LifecycleStage.Should().Be(NodeLifecycleStage.Initializing);
        context.StartedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        context.MachineName.Should().Be(System.Environment.MachineName);
        context.ProcessId.Should().Be(System.Environment.ProcessId);
        context.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithTags_CreatesContextWithTags()
    {
        // Arrange
        var tags = new Dictionary<string, string>
        {
            ["region"] = "us-east",
            ["tier"] = "premium",
        };

        // Act
        var context = new NodeContext("node", "1.0.0", "studio", "env", tags);

        // Assert
        context.Tags.Should().HaveCount(2);
        context.Tags.Should().ContainKey("region").WhoseValue.Should().Be("us-east");
        context.Tags.Should().ContainKey("tier").WhoseValue.Should().Be("premium");
    }

    [Theory]
    [InlineData("", "1.0.0", "studio", "env")]
    [InlineData("node", "", "studio", "env")]
    [InlineData("node", "1.0.0", "", "env")]
    [InlineData("node", "1.0.0", "studio", "")]
    public void Constructor_NullOrWhitespaceParameters_ThrowsArgumentException(
        string nodeId,
        string version,
        string studioId,
        string environment)
    {
        // Act
        var act = () => new NodeContext(nodeId, version, studioId, environment);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetLifecycleStage_UpdatesStage()
    {
        // Arrange
        var context = new NodeContext("node", "1.0.0", "studio", "env");

        // Act
        context.SetLifecycleStage(NodeLifecycleStage.Starting);

        // Assert
        context.LifecycleStage.Should().Be(NodeLifecycleStage.Starting);
    }

    [Fact]
    public void SetLifecycleStage_CanTransitionThroughAllStages()
    {
        // Arrange
        var context = new NodeContext("node", "1.0.0", "studio", "env");

        // Act & Assert
        context.LifecycleStage.Should().Be(NodeLifecycleStage.Initializing);

        context.SetLifecycleStage(NodeLifecycleStage.Starting);
        context.LifecycleStage.Should().Be(NodeLifecycleStage.Starting);

        context.SetLifecycleStage(NodeLifecycleStage.Running);
        context.LifecycleStage.Should().Be(NodeLifecycleStage.Running);

        context.SetLifecycleStage(NodeLifecycleStage.Degraded);
        context.LifecycleStage.Should().Be(NodeLifecycleStage.Degraded);

        context.SetLifecycleStage(NodeLifecycleStage.Stopping);
        context.LifecycleStage.Should().Be(NodeLifecycleStage.Stopping);

        context.SetLifecycleStage(NodeLifecycleStage.Stopped);
        context.LifecycleStage.Should().Be(NodeLifecycleStage.Stopped);
    }

    [Fact]
    public void SetLifecycleStage_CanSetFailedState()
    {
        // Arrange
        var context = new NodeContext("node", "1.0.0", "studio", "env");

        // Act
        context.SetLifecycleStage(NodeLifecycleStage.Failed);

        // Assert
        context.LifecycleStage.Should().Be(NodeLifecycleStage.Failed);
    }
}
