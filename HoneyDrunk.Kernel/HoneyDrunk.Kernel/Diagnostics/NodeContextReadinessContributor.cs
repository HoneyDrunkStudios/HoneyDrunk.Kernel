using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;

namespace HoneyDrunk.Kernel.Diagnostics;

/// <summary>
/// Readiness contributor that ensures Node context is properly initialized.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NodeContextReadinessContributor"/> class.
/// </remarks>
/// <param name="nodeContext">The Node context.</param>
public sealed class NodeContextReadinessContributor(INodeContext nodeContext) : IReadinessContributor
{
    private readonly INodeContext _nodeContext = nodeContext ?? throw new ArgumentNullException(nameof(nodeContext));

    /// <inheritdoc />
    public string Name => "node-context";

    /// <inheritdoc />
    public int Priority => 0; // Run first

    /// <inheritdoc />
    public bool IsRequired => true;

    /// <inheritdoc />
    public Task<(bool isReady, string? reason)> CheckReadinessAsync(CancellationToken cancellationToken = default)
    {
        // Check that Node context has valid data
        if (string.IsNullOrWhiteSpace(_nodeContext.NodeId))
        {
            return CreateResult(false, "NodeId is not set");
        }

        if (string.IsNullOrWhiteSpace(_nodeContext.Version))
        {
            return CreateResult(false, "Version is not set");
        }

        if (string.IsNullOrWhiteSpace(_nodeContext.StudioId))
        {
            return CreateResult(false, "StudioId is not set");
        }

        // Check that Node is in an appropriate stage for readiness
        var stage = _nodeContext.LifecycleStage;
        if (stage is NodeLifecycleStage.Initializing or NodeLifecycleStage.Starting)
        {
            return CreateResult(false, $"Node is still {stage}");
        }

        if (stage is NodeLifecycleStage.Failed or NodeLifecycleStage.Stopped or NodeLifecycleStage.Stopping)
        {
            return CreateResult(false, $"Node is {stage}");
        }

        return CreateResult(true, null);
    }

    private static Task<(bool isReady, string? reason)> CreateResult(bool isReady, string? reason)
        => Task.FromResult((isReady, reason));
}
