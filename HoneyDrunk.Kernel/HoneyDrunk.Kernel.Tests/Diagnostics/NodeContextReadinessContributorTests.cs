using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Diagnostics;

namespace HoneyDrunk.Kernel.Tests.Diagnostics;

public class NodeContextReadinessContributorTests
{
    [Fact]
    public void Constructor_NullNodeContext_ThrowsArgumentNullException()
    {
        var act = () => new NodeContextReadinessContributor(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        var nodeContext = CreateValidNodeContext();
        var contributor = new NodeContextReadinessContributor(nodeContext);

        contributor.Name.Should().Be("node-context");
    }

    [Fact]
    public void Priority_ReturnsZero()
    {
        var nodeContext = CreateValidNodeContext();
        var contributor = new NodeContextReadinessContributor(nodeContext);

        contributor.Priority.Should().Be(0);
    }

    [Fact]
    public void IsRequired_ReturnsTrue()
    {
        var nodeContext = CreateValidNodeContext();
        var contributor = new NodeContextReadinessContributor(nodeContext);

        contributor.IsRequired.Should().BeTrue();
    }

    [Fact]
    public async Task CheckReadinessAsync_ValidContextAndRunningStage_ReturnsReady()
    {
        var nodeContext = CreateValidNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Running);
        var contributor = new NodeContextReadinessContributor(nodeContext);

        var (isReady, reason) = await contributor.CheckReadinessAsync();

        isReady.Should().BeTrue();
        reason.Should().BeNull();
    }

    [Fact]
    public async Task CheckReadinessAsync_EmptyNodeId_ReturnsNotReady()
    {
        var nodeContext = new TestNodeContext
        {
            NodeId = string.Empty,
            Version = "1.0.0",
            StudioId = "test-studio",
            Environment = "test"
        };
        var contributor = new NodeContextReadinessContributor(nodeContext);

        var (isReady, reason) = await contributor.CheckReadinessAsync();

        isReady.Should().BeFalse();
        reason.Should().Be("NodeId is not set");
    }

    [Fact]
    public async Task CheckReadinessAsync_WhitespaceNodeId_ReturnsNotReady()
    {
        var nodeContext = new TestNodeContext
        {
            NodeId = "   ",
            Version = "1.0.0",
            StudioId = "test-studio",
            Environment = "test"
        };
        var contributor = new NodeContextReadinessContributor(nodeContext);

        var (isReady, reason) = await contributor.CheckReadinessAsync();

        isReady.Should().BeFalse();
        reason.Should().Be("NodeId is not set");
    }

    [Fact]
    public async Task CheckReadinessAsync_EmptyVersion_ReturnsNotReady()
    {
        var nodeContext = new TestNodeContext
        {
            NodeId = "test-node",
            Version = string.Empty,
            StudioId = "test-studio",
            Environment = "test"
        };
        var contributor = new NodeContextReadinessContributor(nodeContext);

        var (isReady, reason) = await contributor.CheckReadinessAsync();

        isReady.Should().BeFalse();
        reason.Should().Be("Version is not set");
    }

    [Fact]
    public async Task CheckReadinessAsync_EmptyStudioId_ReturnsNotReady()
    {
        var nodeContext = new TestNodeContext
        {
            NodeId = "test-node",
            Version = "1.0.0",
            StudioId = string.Empty,
            Environment = "test"
        };
        var contributor = new NodeContextReadinessContributor(nodeContext);

        var (isReady, reason) = await contributor.CheckReadinessAsync();

        isReady.Should().BeFalse();
        reason.Should().Be("StudioId is not set");
    }

    [Fact]
    public async Task CheckReadinessAsync_InitializingStage_ReturnsNotReady()
    {
        var nodeContext = CreateValidNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Initializing);
        var contributor = new NodeContextReadinessContributor(nodeContext);

        var (isReady, reason) = await contributor.CheckReadinessAsync();

        isReady.Should().BeFalse();
        reason.Should().Be("Node is still Initializing");
    }

    [Fact]
    public async Task CheckReadinessAsync_StartingStage_ReturnsNotReady()
    {
        var nodeContext = CreateValidNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Starting);
        var contributor = new NodeContextReadinessContributor(nodeContext);

        var (isReady, reason) = await contributor.CheckReadinessAsync();

        isReady.Should().BeFalse();
        reason.Should().Be("Node is still Starting");
    }

    [Fact]
    public async Task CheckReadinessAsync_FailedStage_ReturnsNotReady()
    {
        var nodeContext = CreateValidNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Failed);
        var contributor = new NodeContextReadinessContributor(nodeContext);

        var (isReady, reason) = await contributor.CheckReadinessAsync();

        isReady.Should().BeFalse();
        reason.Should().Be("Node is Failed");
    }

    [Fact]
    public async Task CheckReadinessAsync_StoppedStage_ReturnsNotReady()
    {
        var nodeContext = CreateValidNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Stopped);
        var contributor = new NodeContextReadinessContributor(nodeContext);

        var (isReady, reason) = await contributor.CheckReadinessAsync();

        isReady.Should().BeFalse();
        reason.Should().Be("Node is Stopped");
    }

    [Fact]
    public async Task CheckReadinessAsync_StoppingStage_ReturnsNotReady()
    {
        var nodeContext = CreateValidNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Stopping);
        var contributor = new NodeContextReadinessContributor(nodeContext);

        var (isReady, reason) = await contributor.CheckReadinessAsync();

        isReady.Should().BeFalse();
        reason.Should().Be("Node is Stopping");
    }

    [Fact]
    public async Task CheckReadinessAsync_DegradedStage_ReturnsReady()
    {
        var nodeContext = CreateValidNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Degraded);
        var contributor = new NodeContextReadinessContributor(nodeContext);

        var (isReady, reason) = await contributor.CheckReadinessAsync();

        isReady.Should().BeTrue();
        reason.Should().BeNull();
    }

    [Fact]
    public async Task CheckReadinessAsync_WithCancellationToken_DoesNotThrow()
    {
        var nodeContext = CreateValidNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Running);
        var contributor = new NodeContextReadinessContributor(nodeContext);
        using var cts = new CancellationTokenSource();

        var act = async () => await contributor.CheckReadinessAsync(cts.Token);

        await act.Should().NotThrowAsync();
    }

    private static TestNodeContext CreateValidNodeContext()
    {
        return new TestNodeContext
        {
            NodeId = "test-node",
            Version = "1.0.0",
            StudioId = "test-studio",
            Environment = "test"
        };
    }

    private sealed class TestNodeContext : INodeContext
    {
        public required string NodeId { get; init; }

        public required string Version { get; init; }

        public required string StudioId { get; init; }

        public required string Environment { get; init; }

        public NodeLifecycleStage LifecycleStage { get; private set; } = NodeLifecycleStage.Initializing;

        public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;

        public string MachineName { get; } = System.Environment.MachineName;

        public int ProcessId { get; } = System.Environment.ProcessId;

        public IReadOnlyDictionary<string, string> Tags { get; } = new Dictionary<string, string>();

        public void SetLifecycleStage(NodeLifecycleStage stage)
        {
            LifecycleStage = stage;
        }
    }
}
