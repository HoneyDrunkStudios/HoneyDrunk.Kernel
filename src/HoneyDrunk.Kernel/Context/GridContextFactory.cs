using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Context;

/// <summary>
/// Factory for creating child GridContext instances for cross-node propagation.
/// </summary>
/// <remarks>
/// This factory creates initialized child contexts for scenarios where context must be
/// serialized and sent to another node. It does NOT create root contexts - those are
/// created by DI scope initialization.
/// </remarks>
public sealed class GridContextFactory : IGridContextFactory
{
    /// <inheritdoc />
    public IGridContext CreateChild(
        IGridContext parent,
        IOperationContext operation,
        string? nodeId = null,
        IReadOnlyDictionary<string, string>? extraBaggage = null)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(operation);

        if (!parent.IsInitialized)
        {
            throw new InvalidOperationException(
                "Cannot create child context from uninitialized parent GridContext.");
        }

        // Merge parent baggage with extra baggage
        Dictionary<string, string>? mergedBaggage = null;
        if (parent.Baggage.Count > 0 || extraBaggage?.Count > 0)
        {
            mergedBaggage = new Dictionary<string, string>(parent.Baggage);
            if (extraBaggage != null)
            {
                foreach (var kvp in extraBaggage)
                {
                    mergedBaggage[kvp.Key] = kvp.Value;
                }
            }
        }

        // Create a new context for cross-node propagation
        // This context is pre-initialized (it's for serialization, not local use)
        var effectiveNodeId = nodeId ?? parent.NodeId;
        var child = new GridContext(effectiveNodeId, parent.StudioId, parent.Environment);

        child.Initialize(
            correlationId: parent.CorrelationId,
            causationId: operation.OperationId, // Key: causation chain links to parent operation
            tenantId: parent.TenantId,
            projectId: parent.ProjectId,
            baggage: mergedBaggage,
            cancellation: parent.Cancellation);

        return child;
    }
}
