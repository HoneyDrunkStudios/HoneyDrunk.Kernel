using HoneyDrunk.Kernel.Abstractions.Agents;
using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.AgentsInterop;

/// <summary>
/// Projects GridContext into IAgentExecutionContext for AI agent consumption.
/// </summary>
public static class AgentContextProjection
{
    /// <summary>
    /// Projects a GridContext and agent descriptor into an AgentExecutionContext.
    /// </summary>
    /// <param name="gridContext">The Grid context.</param>
    /// <param name="operationContext">The operation context.</param>
    /// <param name="agentDescriptor">The agent descriptor.</param>
    /// <param name="executionMetadata">Optional execution-specific metadata.</param>
    /// <returns>An IAgentExecutionContext ready for agent execution.</returns>
    public static IAgentExecutionContext ProjectToAgentContext(
        IGridContext gridContext,
        IOperationContext operationContext,
        IAgentDescriptor agentDescriptor,
        IReadOnlyDictionary<string, object?>? executionMetadata = null)
    {
        ArgumentNullException.ThrowIfNull(gridContext);
        ArgumentNullException.ThrowIfNull(operationContext);
        ArgumentNullException.ThrowIfNull(agentDescriptor);

        return new AgentExecutionContext(
            gridContext,
            operationContext,
            agentDescriptor,
            executionMetadata ?? new Dictionary<string, object?>());
    }

    /// <summary>
    /// Default implementation of IAgentExecutionContext.
    /// </summary>
    private sealed class AgentExecutionContext(
        IGridContext gridContext,
        IOperationContext operationContext,
        IAgentDescriptor agent,
        IReadOnlyDictionary<string, object?> executionMetadata) : IAgentExecutionContext
    {
        private readonly Dictionary<string, object?> _metadata = new(executionMetadata);

        public IGridContext GridContext { get; } = gridContext;

        public IOperationContext OperationContext { get; } = operationContext;

        public IAgentDescriptor Agent { get; } = agent;

        public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;

        public IReadOnlyDictionary<string, object?> ExecutionMetadata => _metadata;

        public void AddMetadata(string key, object? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            _metadata[key] = value;
        }

        public bool CanAccess(string resourceType, string resourceId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

            // Default implementation: check if agent has capability for resource type
            // Downstream implementations can override with more sophisticated RBAC
            var capability = $"access:{resourceType}";
            return Agent.HasCapability(capability);
        }
    }
}
