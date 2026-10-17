using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Health;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;

namespace HoneyDrunk.Kernel.Diagnostics;

/// <summary>
/// Health contributor that checks if the Node is in a healthy lifecycle stage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NodeLifecycleHealthContributor"/> class.
/// </remarks>
/// <param name="nodeContext">The Node context.</param>
public sealed class NodeLifecycleHealthContributor(INodeContext nodeContext) : IHealthContributor
{
    private readonly INodeContext _nodeContext = nodeContext ?? throw new ArgumentNullException(nameof(nodeContext));

    /// <inheritdoc />
    public string Name => "node-lifecycle";

    /// <inheritdoc />
    public int Priority => 0; // Run first

    /// <inheritdoc />
    public bool IsCritical => true;

    /// <inheritdoc />
    public Task<(HealthStatus status, string? message)> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stage = _nodeContext.LifecycleStage;

        return stage switch
        {
            NodeLifecycleStage.Ready => CreateResult(HealthStatus.Healthy, "Node is ready"),
            NodeLifecycleStage.Degraded => CreateResult(HealthStatus.Degraded, "Node is degraded"),
            NodeLifecycleStage.Failed => CreateResult(HealthStatus.Unhealthy, "Node has failed"),
            NodeLifecycleStage.Stopping => CreateResult(HealthStatus.Unhealthy, "Node is stopping"),
            NodeLifecycleStage.Stopped => CreateResult(HealthStatus.Unhealthy, "Node is stopped"),
            NodeLifecycleStage.Initializing => CreateResult(HealthStatus.Degraded, "Node is initializing"),
            NodeLifecycleStage.Starting => CreateResult(HealthStatus.Degraded, "Node is starting"),
            _ => CreateResult(HealthStatus.Degraded, $"Node is in {stage} stage")
        };
    }

    private static Task<(HealthStatus status, string? message)> CreateResult(HealthStatus status, string? message)
        => Task.FromResult((status, message));
}
