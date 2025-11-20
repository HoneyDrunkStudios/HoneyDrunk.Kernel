namespace HoneyDrunk.Kernel.Abstractions.Agents;

/// <summary>
/// Defines a capability that an agent can perform.
/// </summary>
/// <remarks>
/// Capabilities are declarative descriptions of what an agent can do.
/// They enable capability-based security and dynamic tool discovery.
/// Examples: "read-secrets", "invoke-api", "query-database", "send-notifications".
/// </remarks>
public interface IAgentCapability
{
    /// <summary>
    /// Gets the unique name of this capability.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the human-readable description of this capability.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the category/domain of this capability.
    /// Examples: "data-access", "communication", "computation", "integration".
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets the required permission level for this capability.
    /// Examples: "read", "write", "admin".
    /// </summary>
    string PermissionLevel { get; }

    /// <summary>
    /// Gets additional constraints or requirements for this capability.
    /// Examples: rate limits, resource quotas, time windows.
    /// </summary>
    IReadOnlyDictionary<string, string> Constraints { get; }

    /// <summary>
    /// Validates if the capability can be invoked with the given parameters.
    /// </summary>
    /// <param name="parameters">The parameters for the capability invocation.</param>
    /// <param name="errorMessage">The validation error message if validation fails.</param>
    /// <returns>True if parameters are valid; otherwise false.</returns>
    bool ValidateParameters(IReadOnlyDictionary<string, object?> parameters, out string? errorMessage);
}
