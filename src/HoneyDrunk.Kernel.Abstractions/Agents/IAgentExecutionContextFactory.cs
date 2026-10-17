using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Abstractions.Agents;

/// <summary>
/// Factory for creating scoped agent execution contexts.
/// </summary>
/// <remarks>
/// Composes GridContext, OperationContext, and AgentDescriptor into a unified execution context
/// for agent operations. This is the primary entry point for creating agent execution contexts
/// in the Grid. The factory ensures consistent execution semantics across all Nodes.
/// </remarks>
public interface IAgentExecutionContextFactory
{
    /// <summary>
    /// Creates a scoped agent execution context.
    /// </summary>
    /// <param name="agent">The agent descriptor defining identity and capabilities.</param>
    /// <param name="gridContext">The Grid context for this execution.</param>
    /// <param name="operationContext">Optional operation context (created if not provided).</param>
    /// <param name="executionMetadata">Optional execution metadata (model, temperature, etc.).</param>
    /// <returns>A scoped agent execution context that should be disposed when execution completes.</returns>
    /// <remarks>
    /// If no operation context is provided, the factory creates one using the Grid context.
    /// This ensures every agent execution is properly tracked in telemetry and observability systems.
    /// The returned context implements IDisposable and should be used in a using block.
    /// </remarks>
    IAgentExecutionContext Create(
        IAgentDescriptor agent,
        IGridContext gridContext,
        IOperationContext? operationContext = null,
        IReadOnlyDictionary<string, object?>? executionMetadata = null);
}
