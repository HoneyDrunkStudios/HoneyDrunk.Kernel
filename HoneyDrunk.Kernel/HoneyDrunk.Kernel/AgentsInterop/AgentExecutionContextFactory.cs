using HoneyDrunk.Kernel.Abstractions.Agents;
using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.AgentsInterop;

/// <summary>
/// Factory for creating scoped agent execution contexts.
/// </summary>
/// <remarks>
/// Composes GridContext, OperationContext, and AgentDescriptor into a unified execution context.
/// This is the primary entry point for creating agent execution contexts in the Grid.
/// Uses AgentContextProjection internally for the actual projection logic.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated via DI in AddHoneyDrunkNode")]
internal sealed class AgentExecutionContextFactory(IOperationContextFactory operationContextFactory) : IAgentExecutionContextFactory
{
    private readonly IOperationContextFactory _operationContextFactory = operationContextFactory ?? throw new ArgumentNullException(nameof(operationContextFactory));

    /// <inheritdoc />
    public IAgentExecutionContext Create(
        IAgentDescriptor agent,
        IGridContext gridContext,
        IOperationContext? operationContext = null,
        IReadOnlyDictionary<string, object?>? executionMetadata = null)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(gridContext);

        // Create operation context if not provided
        // Note: IOperationContextFactory uses current IGridContext from ambient accessor,
        // so we assume gridContext is already set via accessor for factory scenarios.
        // For direct usage, caller must provide operationContext.
        var operation = operationContext ?? _operationContextFactory.Create(
            operationName: $"Agent:{agent.AgentId}");

        // Use AgentContextProjection for the actual composition
        return AgentContextProjection.ProjectToAgentContext(
            gridContext: gridContext,
            operationContext: operation,
            agentDescriptor: agent,
            executionMetadata: executionMetadata);
    }
}
