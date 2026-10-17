using System.Text.Json;

namespace HoneyDrunk.Kernel.AgentsInterop;

/// <summary>
/// Represents a deserialized agent execution result.
/// </summary>
public sealed record AgentExecutionResult
{
    /// <summary>
    /// Gets or initializes the agent identifier.
    /// </summary>
    public string? AgentId { get; init; }

    /// <summary>
    /// Gets or initializes the correlation identifier.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets a value indicating whether gets or initializes a value indicating whether the execution succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or initializes the execution result.
    /// </summary>
    public JsonElement? Result { get; init; }

    /// <summary>
    /// Gets or initializes the error message.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or initializes the execution start time.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; init; }

    /// <summary>
    /// Gets or initializes the execution completion time.
    /// </summary>
    public DateTimeOffset CompletedAtUtc { get; init; }

    /// <summary>
    /// Gets or initializes the execution metadata.
    /// </summary>
    public Dictionary<string, JsonElement>? Metadata { get; init; }
}
