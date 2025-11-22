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
            NodeLifecycleStage.Running => Task.FromResult((status: HealthStatus.Healthy, message: (string?)"Node is running")),
            NodeLifecycleStage.Degraded => Task.FromResult((status: HealthStatus.Degraded, message: (string?)"Node is degraded")),
            NodeLifecycleStage.Failed => Task.FromResult((status: HealthStatus.Unhealthy, message: (string?)"Node has failed")),
            NodeLifecycleStage.Stopping => Task.FromResult((status: HealthStatus.Unhealthy, message: (string?)"Node is stopping")),
            NodeLifecycleStage.Stopped => Task.FromResult((status: HealthStatus.Unhealthy, message: (string?)"Node is stopped")),
            NodeLifecycleStage.Initializing => Task.FromResult((status: HealthStatus.Degraded, message: (string?)"Node is initializing")),
            NodeLifecycleStage.Starting => Task.FromResult((status: HealthStatus.Degraded, message: (string?)"Node is starting")),
            _ => Task.FromResult((status: HealthStatus.Degraded, message: (string?)$"Node is in {stage} stage"))
        };
    }
}
