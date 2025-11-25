using HoneyDrunk.Kernel.Abstractions.Agents;
using System.Text.Json;

namespace HoneyDrunk.Kernel.AgentsInterop;

/// <summary>
/// Serializes agent execution results for Grid consumption.
/// </summary>
public sealed class AgentResultSerializer
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes an agent execution result.
    /// </summary>
    /// <param name="context">The agent execution context.</param>
    /// <param name="success">Whether the execution succeeded.</param>
    /// <param name="result">The execution result.</param>
    /// <param name="errorMessage">The error message if execution failed.</param>
    /// <returns>A JSON string representing the execution result.</returns>
    public static string SerializeResult(
        IAgentExecutionContext context,
        bool success,
        object? result = null,
        string? errorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var data = new
        {
            agentId = context.Agent.AgentId,
            correlationId = context.GridContext.CorrelationId,
            success,
            result,
            errorMessage,
            startedAtUtc = context.StartedAtUtc,
            completedAtUtc = DateTimeOffset.UtcNow,
            metadata = context.ExecutionMetadata,
        };

        return JsonSerializer.Serialize(data, _jsonOptions);
    }

    /// <summary>
    /// Deserializes an agent execution result.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized agent execution result, or null if deserialization fails.</returns>
    public static AgentExecutionResult? DeserializeResult(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json, nameof(json));

        try
        {
            return JsonSerializer.Deserialize<AgentExecutionResult>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
