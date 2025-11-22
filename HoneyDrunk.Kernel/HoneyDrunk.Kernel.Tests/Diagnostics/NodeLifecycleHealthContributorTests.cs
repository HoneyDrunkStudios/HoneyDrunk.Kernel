using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Health;
using HoneyDrunk.Kernel.Diagnostics;

namespace HoneyDrunk.Kernel.Tests.Diagnostics;

public class NodeLifecycleHealthContributorTests
{
    [Fact]
    public void Constructor_NullNodeContext_ThrowsArgumentNullException()
    {
        var act = () => new NodeLifecycleHealthContributor(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        var nodeContext = CreateTestNodeContext();
        var contributor = new NodeLifecycleHealthContributor(nodeContext);

        contributor.Name.Should().Be("node-lifecycle");
    }

    [Fact]
    public void Priority_ReturnsZero()
    {
        var nodeContext = CreateTestNodeContext();
        var contributor = new NodeLifecycleHealthContributor(nodeContext);

        contributor.Priority.Should().Be(0);
    }

    [Fact]
    public void IsCritical_ReturnsTrue()
    {
        var nodeContext = CreateTestNodeContext();
        var contributor = new NodeLifecycleHealthContributor(nodeContext);

        contributor.IsCritical.Should().BeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_RunningStage_ReturnsHealthy()
    {
        var nodeContext = CreateTestNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Running);
        var contributor = new NodeLifecycleHealthContributor(nodeContext);

        var (status, message) = await contributor.CheckHealthAsync();

        status.Should().Be(HealthStatus.Healthy);
        message.Should().Be("Node is running");
    }

    [Fact]
    public async Task CheckHealthAsync_DegradedStage_ReturnsDegraded()
    {
        var nodeContext = CreateTestNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Degraded);
        var contributor = new NodeLifecycleHealthContributor(nodeContext);

        var (status, message) = await contributor.CheckHealthAsync();

        status.Should().Be(HealthStatus.Degraded);
        message.Should().Be("Node is degraded");
    }

    [Fact]
    public async Task CheckHealthAsync_FailedStage_ReturnsUnhealthy()
    {
        var nodeContext = CreateTestNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Failed);
        var contributor = new NodeLifecycleHealthContributor(nodeContext);

        var (status, message) = await contributor.CheckHealthAsync();

        status.Should().Be(HealthStatus.Unhealthy);
        message.Should().Be("Node has failed");
    }

    [Fact]
    public async Task CheckHealthAsync_StoppingStage_ReturnsUnhealthy()
    {
        var nodeContext = CreateTestNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Stopping);
        var contributor = new NodeLifecycleHealthContributor(nodeContext);

        var (status, message) = await contributor.CheckHealthAsync();

        status.Should().Be(HealthStatus.Unhealthy);
        message.Should().Be("Node is stopping");
    }

    [Fact]
    public async Task CheckHealthAsync_StoppedStage_ReturnsUnhealthy()
    {
        var nodeContext = CreateTestNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Stopped);
        var contributor = new NodeLifecycleHealthContributor(nodeContext);

        var (status, message) = await contributor.CheckHealthAsync();

        status.Should().Be(HealthStatus.Unhealthy);
        message.Should().Be("Node is stopped");
    }

    [Fact]
    public async Task CheckHealthAsync_InitializingStage_ReturnsDegraded()
    {
        var nodeContext = CreateTestNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Initializing);
        var contributor = new NodeLifecycleHealthContributor(nodeContext);

        var (status, message) = await contributor.CheckHealthAsync();

        status.Should().Be(HealthStatus.Degraded);
        message.Should().Be("Node is initializing");
    }

    [Fact]
    public async Task CheckHealthAsync_StartingStage_ReturnsDegraded()
    {
        var nodeContext = CreateTestNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Starting);
        var contributor = new NodeLifecycleHealthContributor(nodeContext);

        var (status, message) = await contributor.CheckHealthAsync();

        status.Should().Be(HealthStatus.Degraded);
        message.Should().Be("Node is starting");
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellationToken_DoesNotThrow()
    {
        var nodeContext = CreateTestNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Running);
        var contributor = new NodeLifecycleHealthContributor(nodeContext);
        using var cts = new CancellationTokenSource();

        var act = async () => await contributor.CheckHealthAsync(cts.Token);

        await act.Should().NotThrowAsync();
    }

    private static TestNodeContext CreateTestNodeContext()
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
