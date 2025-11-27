namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Factory for creating IGridContext instances with proper causation tracking.
/// </summary>
/// <remarks>
/// This factory is responsible for composing new GridContext instances, particularly
/// for child contexts where the causation chain must reference the parent operation's OperationId.
/// GridContext itself is a pure value object and does not create its own children.
/// </remarks>
public interface IGridContextFactory
{
    /// <summary>
    /// Creates a root Grid context (no parent operation).
    /// </summary>
    /// <param name="nodeId">The Node identifier.</param>
    /// <param name="studioId">The Studio identifier.</param>
    /// <param name="environment">The environment name.</param>
    /// <param name="correlationId">Optional correlation identifier; generates new ULID if not provided.</param>
    /// <param name="causationId">Optional causation identifier (typically null for root contexts).</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="projectId">Optional project identifier.</param>
    /// <param name="baggage">Optional baggage dictionary.</param>
    /// <param name="cancellation">Optional cancellation token.</param>
    /// <returns>A new root GridContext.</returns>
    IGridContext CreateRoot(
        string nodeId,
        string studioId,
        string environment,
        string? correlationId = null,
        string? causationId = null,
        string? tenantId = null,
        string? projectId = null,
        IReadOnlyDictionary<string, string>? baggage = null,
        CancellationToken cancellation = default);

    /// <summary>
    /// Creates a child Grid context from a parent context and the current operation.
    /// </summary>
    /// <param name="parent">The parent Grid context.</param>
    /// <param name="operation">The current operation (provides OperationId for causation tracking).</param>
    /// <param name="nodeId">Optional Node ID if crossing Node boundaries; if null, uses parent's NodeId.</param>
    /// <param name="extraBaggage">Optional additional baggage to merge with parent's baggage.</param>
    /// <returns>A new child GridContext with causationId set to the operation's OperationId.</returns>
    /// <remarks>
    /// The child context preserves:
    /// - CorrelationId (same as parent - constant across trace).
    /// - TenantId (same as parent).
    /// - ProjectId (same as parent).
    /// - Baggage (parent's baggage + extraBaggage).
    /// The child context sets:
    /// - CausationId to operation.OperationId (forms parent-child chain).
    /// </remarks>
    IGridContext CreateChild(
        IGridContext parent,
        IOperationContext operation,
        string? nodeId = null,
        IReadOnlyDictionary<string, string>? extraBaggage = null);
}
