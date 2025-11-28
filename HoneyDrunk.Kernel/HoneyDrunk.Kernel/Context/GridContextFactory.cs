using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Context;

/// <summary>
/// Default implementation of IGridContextFactory.
/// </summary>
public sealed class GridContextFactory : IGridContextFactory
{
    /// <inheritdoc />
    public IGridContext CreateRoot(
        string nodeId,
        string studioId,
        string environment,
        string? correlationId = null,
        string? causationId = null,
        string? tenantId = null,
        string? projectId = null,
        IReadOnlyDictionary<string, string>? baggage = null,
        CancellationToken cancellation = default)
    {
        // Generate correlation ID if not provided
        var correlation = correlationId ?? Ulid.NewUlid().ToString();

        return new GridContext(
            correlationId: correlation,
            nodeId: nodeId,
            studioId: studioId,
            environment: environment,
            causationId: causationId,
            tenantId: tenantId,
            projectId: projectId,
            baggage: baggage,
            cancellation: cancellation);
    }

    /// <inheritdoc />
    public IGridContext CreateChild(
        IGridContext parent,
        IOperationContext operation,
        string? nodeId = null,
        IReadOnlyDictionary<string, string>? extraBaggage = null)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(operation);

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

        // Create child with causation set to parent operation's OperationId
        return new GridContext(
            correlationId: parent.CorrelationId,
            nodeId: nodeId ?? parent.NodeId,
            studioId: parent.StudioId,
            environment: parent.Environment,
            causationId: operation.OperationId,
            tenantId: parent.TenantId,
            projectId: parent.ProjectId,
            baggage: mergedBaggage,
            cancellation: parent.Cancellation);
    }
}
