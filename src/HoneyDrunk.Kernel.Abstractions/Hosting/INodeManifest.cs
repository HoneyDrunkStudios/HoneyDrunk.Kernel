namespace HoneyDrunk.Kernel.Abstractions.Hosting;

/// <summary>
/// Represents a Node's manifest defining its identity, capabilities, dependencies, and configuration shape.
/// </summary>
/// <remarks>
/// The Node manifest is the declarative contract that describes what a Node is and what it needs.
/// It serves as both runtime metadata and documentation. Manifests can be:
/// - Embedded in the Node binary
/// - Loaded from configuration files
/// - Registered with service discovery
/// - Used for dependency validation at startup.
/// </remarks>
public interface INodeManifest
{
    /// <summary>
    /// Gets the Node identifier.
    /// Example: "payment-node", "notification-node", "auth-gateway".
    /// </summary>
    string NodeId { get; }

    /// <summary>
    /// Gets the semantic version of this Node.
    /// Example: "1.2.3", "2.0.0-beta.1".
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the human-readable description of this Node's purpose.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the capabilities/features this Node provides.
    /// Example: ["payment-processing", "refund-handling", "webhook-notifications"].
    /// </summary>
    IReadOnlyList<string> Capabilities { get; }

    /// <summary>
    /// Gets the Node IDs or capabilities this Node depends on.
    /// Example: ["database-node", "auth-gateway"].
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Gets the configuration schema requirements for this Node.
    /// Keys represent required configuration paths, values represent expected types or constraints.
    /// </summary>
    IReadOnlyDictionary<string, string> ConfigurationSchema { get; }

    /// <summary>
    /// Gets the Node-specific tags/labels for capability advertisement and routing.
    /// Example: { "protocol": "http", "data-region": "us-east", "tier": "premium" }.
    /// </summary>
    IReadOnlyDictionary<string, string> Tags { get; }

    /// <summary>
    /// Gets health check endpoints or validation rules this Node exposes.
    /// </summary>
    IReadOnlyList<string> HealthCheckEndpoints { get; }

    /// <summary>
    /// Gets the minimum lifecycle stage required for dependent Nodes before this Node can start.
    /// </summary>
    string? RequiredDependencyStage { get; }
}
