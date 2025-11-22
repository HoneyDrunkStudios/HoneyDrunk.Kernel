using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Health;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Kernel.Lifecycle;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Kernel.Tests.Lifecycle;

public class NodeLifecycleManagerTests
{
    [Fact]
    public void Constructor_NullNodeContext_ThrowsArgumentNullException()
    {
        var act = () => new NodeLifecycleManager(
            null!,
            [],
            [],
            NullLogger<NodeLifecycleManager>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullHealthContributors_ThrowsArgumentNullException()
    {
        var nodeContext = CreateTestNodeContext();

        var act = () => new NodeLifecycleManager(
            nodeContext,
            null!,
            [],
            NullLogger<NodeLifecycleManager>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullReadinessContributors_ThrowsArgumentNullException()
    {
        var nodeContext = CreateTestNodeContext();

        var act = () => new NodeLifecycleManager(
            nodeContext,
            [],
            null!,
            NullLogger<NodeLifecycleManager>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var nodeContext = CreateTestNodeContext();

        var act = () => new NodeLifecycleManager(
            nodeContext,
            [],
            [],
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckHealthAsync_NoContributors_ReturnsHealthy()
    {
        var manager = CreateManager();

        var (status, details) = await manager.CheckHealthAsync();

        status.Should().Be(HealthStatus.Healthy);
        details.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckHealthAsync_AllHealthy_ReturnsHealthy()
    {
        var contributors = new[]
        {
            new TestHealthContributor("check1", HealthStatus.Healthy),
            new TestHealthContributor("check2", HealthStatus.Healthy)
        };
        var manager = CreateManager(healthContributors: contributors);

        var (status, details) = await manager.CheckHealthAsync();

        status.Should().Be(HealthStatus.Healthy);
        details.Should().HaveCount(2);
        details["check1"].status.Should().Be(HealthStatus.Healthy);
        details["check2"].status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_OneDegraded_ReturnsDegraded()
    {
        var contributors = new[]
        {
            new TestHealthContributor("check1", HealthStatus.Healthy),
            new TestHealthContributor("check2", HealthStatus.Degraded)
        };
        var manager = CreateManager(healthContributors: contributors);

        var (status, details) = await manager.CheckHealthAsync();

        status.Should().Be(HealthStatus.Degraded);
        details.Should().HaveCount(2);
    }

    [Fact]
    public async Task CheckHealthAsync_OneUnhealthy_ReturnsUnhealthy()
    {
        var contributors = new[]
        {
            new TestHealthContributor("check1", HealthStatus.Healthy),
            new TestHealthContributor("check2", HealthStatus.Unhealthy)
        };
        var manager = CreateManager(healthContributors: contributors);
        var (status, _) = await manager.CheckHealthAsync();

        status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_CriticalUnhealthy_StopsEarly()
    {
        var contributors = new[]
        {
            new TestHealthContributor("check1", HealthStatus.Unhealthy, isCritical: true, priority: 1),
            new TestHealthContributor("check2", HealthStatus.Healthy, priority: 2)
        };
        var manager = CreateManager(healthContributors: contributors);

        var (status, details) = await manager.CheckHealthAsync();

        status.Should().Be(HealthStatus.Unhealthy);
        details.Should().HaveCount(1);
        details.Should().ContainKey("check1");
        details.Should().NotContainKey("check2");
    }

    [Fact]
    public async Task CheckHealthAsync_NonCriticalUnhealthy_ContinuesChecking()
    {
        var contributors = new[]
        {
            new TestHealthContributor("check1", HealthStatus.Unhealthy, isCritical: false, priority: 1),
            new TestHealthContributor("check2", HealthStatus.Healthy, priority: 2)
        };
        var manager = CreateManager(healthContributors: contributors);

        var (status, details) = await manager.CheckHealthAsync();

        status.Should().Be(HealthStatus.Unhealthy);
        details.Should().HaveCount(2);
    }

    [Fact]
    public async Task CheckHealthAsync_ContributorThrowsException_ReturnsUnhealthy()
    {
        var contributors = new IHealthContributor[]
        {
            new ThrowingHealthContributor("failing-check", new InvalidOperationException("Test error"))
        };
        var manager = CreateManager(healthContributors: contributors);

        var (status, details) = await manager.CheckHealthAsync();

        status.Should().Be(HealthStatus.Unhealthy);
        details["failing-check"].status.Should().Be(HealthStatus.Unhealthy);
        details["failing-check"].message.Should().Contain("Exception");
    }

    [Fact]
    public async Task CheckHealthAsync_OrdersByPriority()
    {
        var executionOrder = new List<string>();
        var contributors = new[]
        {
            new OrderTrackingHealthContributor("check-low", HealthStatus.Healthy, 10, executionOrder),
            new OrderTrackingHealthContributor("check-high", HealthStatus.Healthy, 1, executionOrder),
            new OrderTrackingHealthContributor("check-mid", HealthStatus.Healthy, 5, executionOrder)
        };
        var manager = CreateManager(healthContributors: contributors);

        await manager.CheckHealthAsync();

        executionOrder.Should().Equal("check-high", "check-mid", "check-low");
    }

    [Fact]
    public async Task CheckReadinessAsync_NoContributors_ReturnsReady()
    {
        var manager = CreateManager();

        var (isReady, details) = await manager.CheckReadinessAsync();

        isReady.Should().BeTrue();
        details.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckReadinessAsync_AllReady_ReturnsReady()
    {
        var contributors = new[]
        {
            new TestReadinessContributor("check1", isReady: true),
            new TestReadinessContributor("check2", isReady: true)
        };
        var manager = CreateManager(readinessContributors: contributors);

        var (isReady, details) = await manager.CheckReadinessAsync();

        isReady.Should().BeTrue();
        details.Should().HaveCount(2);
    }

    [Fact]
    public async Task CheckReadinessAsync_RequiredNotReady_ReturnsNotReady()
    {
        var contributors = new[]
        {
            new TestReadinessContributor("check1", isReady: true),
            new TestReadinessContributor("check2", isReady: false, isRequired: true)
        };
        var manager = CreateManager(readinessContributors: contributors);
        var (isReady, _) = await manager.CheckReadinessAsync();

        isReady.Should().BeFalse();
    }

    [Fact]
    public async Task CheckReadinessAsync_OptionalNotReady_ReturnsReady()
    {
        var contributors = new[]
        {
            new TestReadinessContributor("check1", isReady: true, isRequired: true),
            new TestReadinessContributor("check2", isReady: false, isRequired: false)
        };
        var manager = CreateManager(readinessContributors: contributors);

        var (isReady, details) = await manager.CheckReadinessAsync();

        isReady.Should().BeTrue();
        details.Should().HaveCount(2);
    }

    [Fact]
    public async Task CheckReadinessAsync_ContributorThrowsException_ReturnsNotReadyIfRequired()
    {
        var contributors = new IReadinessContributor[]
        {
            new ThrowingReadinessContributor("failing-check", new InvalidOperationException("Test error"), isRequired: true)
        };
        var manager = CreateManager(readinessContributors: contributors);

        var (isReady, details) = await manager.CheckReadinessAsync();

        isReady.Should().BeFalse();
        details["failing-check"].isReady.Should().BeFalse();
        details["failing-check"].reason.Should().Contain("Exception");
    }

    [Fact]
    public void TransitionToStage_UpdatesNodeContextStage()
    {
        var nodeContext = CreateTestNodeContext();
        var manager = CreateManager(nodeContext);

        manager.TransitionToStage(NodeLifecycleStage.Running);

        nodeContext.LifecycleStage.Should().Be(NodeLifecycleStage.Running);
    }

    [Fact]
    public void TransitionToStage_SameStage_DoesNothing()
    {
        var nodeContext = CreateTestNodeContext();
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Running);
        var manager = CreateManager(nodeContext);

        manager.TransitionToStage(NodeLifecycleStage.Running);

        nodeContext.LifecycleStage.Should().Be(NodeLifecycleStage.Running);
    }

    private static NodeLifecycleManager CreateManager(
        TestNodeContext? nodeContext = null,
        IEnumerable<IHealthContributor>? healthContributors = null,
        IEnumerable<IReadinessContributor>? readinessContributors = null)
    {
        return new NodeLifecycleManager(
            nodeContext ?? CreateTestNodeContext(),
            healthContributors ?? [],
            readinessContributors ?? [],
            NullLogger<NodeLifecycleManager>.Instance);
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

    private sealed class TestHealthContributor(
        string name,
        HealthStatus status,
        bool isCritical = false,
        int priority = 0,
        string? message = null) : IHealthContributor
    {
        public string Name => name;

        public int Priority => priority;

        public bool IsCritical => isCritical;

        public Task<(HealthStatus status, string? message)> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((status, message));
        }
    }

    private sealed class ThrowingHealthContributor(string name, Exception exception, bool isCritical = false) : IHealthContributor
    {
        public string Name => name;

        public int Priority => 0;

        public bool IsCritical => isCritical;

        public Task<(HealthStatus status, string? message)> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            throw exception;
        }
    }

    private sealed class OrderTrackingHealthContributor(
        string name,
        HealthStatus status,
        int priority,
        List<string> executionOrder) : IHealthContributor
    {
        public string Name => name;

        public int Priority => priority;

        public bool IsCritical => false;

        public Task<(HealthStatus status, string? message)> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            executionOrder.Add(name);
            return Task.FromResult((status, (string?)null));
        }
    }

    private sealed class TestReadinessContributor(
        string name,
        bool isReady,
        bool isRequired = true,
        int priority = 0,
        string? reason = null) : IReadinessContributor
    {
        public string Name => name;

        public int Priority => priority;

        public bool IsRequired => isRequired;

        public Task<(bool isReady, string? reason)> CheckReadinessAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((isReady, reason));
        }
    }

    private sealed class ThrowingReadinessContributor(string name, Exception exception, bool isRequired = true) : IReadinessContributor
    {
        public string Name => name;

        public int Priority => 0;

        public bool IsRequired => isRequired;

        public Task<(bool isReady, string? reason)> CheckReadinessAsync(CancellationToken cancellationToken = default)
        {
            throw exception;
        }
    }
}
