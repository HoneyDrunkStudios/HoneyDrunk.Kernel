namespace HoneyDrunk.Kernel.Abstractions.Configuration;

/// <summary>
/// Defines the scope/context for configuration access.
/// </summary>
/// <remarks>
/// Configuration scopes enable hierarchical and contextual configuration:
/// - Global: Shared across all Studios and Nodes.
/// - Studio: Shared across all Nodes in a Studio.
/// - Node: Specific to a Node instance.
/// - Tenant: Tenant-specific configuration.
/// - Request: Request/operation-specific overrides.
/// </remarks>
public interface IConfigScope
{
    /// <summary>
    /// Gets the scope type.
    /// </summary>
    ConfigScopeType ScopeType { get; }

    /// <summary>
    /// Gets the scope identifier (e.g., Studio ID, Node ID, Tenant ID).
    /// </summary>
    string? ScopeId { get; }

    /// <summary>
    /// Gets the parent scope (if this is a nested scope).
    /// </summary>
    IConfigScope? ParentScope { get; }

    /// <summary>
    /// Gets the full scope path (e.g., "global/studio/node").
    /// </summary>
    string ScopePath { get; }

    /// <summary>
    /// Creates a child scope.
    /// </summary>
    /// <param name="scopeType">The child scope type.</param>
    /// <param name="scopeId">The child scope identifier.</param>
    /// <returns>A new child scope.</returns>
    IConfigScope CreateChildScope(ConfigScopeType scopeType, string scopeId);
}
