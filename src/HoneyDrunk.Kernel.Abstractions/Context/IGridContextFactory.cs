namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Factory for creating child GridContext instances for cross-node propagation.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates child contexts when work crosses node boundaries (e.g., sending a message
/// to another service, making an outbound HTTP call). It establishes proper causation tracking
/// by setting the child's CausationId to the parent operation's OperationId.
/// </para>
/// <para>
/// <strong>Note:</strong> This factory does NOT create root contexts. Root context creation
/// is handled by DI scope initialization. Use this factory only when propagating context
/// to external systems or spawning tracked background work.
/// </para>
/// </remarks>
public interface IGridContextFactory
{
    /// <summary>
    /// Creates a child Grid context from a parent context and the current operation.
    /// </summary>
    /// <param name="parent">The parent Grid context.</param>
    /// <param name="operation">The current operation (provides OperationId for causation tracking).</param>
    /// <param name="nodeId">Optional Node ID if crossing Node boundaries; if null, uses parent's NodeId.</param>
    /// <param name="extraBaggage">Optional additional baggage to merge with parent's baggage.</param>
    /// <returns>A new child GridContext with causationId set to the operation's OperationId.</returns>
    /// <remarks>
    /// <para>
    /// The child context preserves:
    /// </para>
    /// <list type="bullet">
    /// <item>CorrelationId (same as parent - constant across trace)</item>
    /// <item>TenantId (same as parent)</item>
    /// <item>ProjectId (same as parent)</item>
    /// <item>Baggage (parent's baggage merged with extraBaggage)</item>
    /// </list>
    /// <para>
    /// The child context sets:
    /// </para>
    /// <list type="bullet">
    /// <item>CausationId to operation.OperationId (forms parent-child chain)</item>
    /// </list>
    /// </remarks>
    IGridContext CreateChild(
        IGridContext parent,
        IOperationContext operation,
        string? nodeId = null,
        IReadOnlyDictionary<string, string>? extraBaggage = null);
}
