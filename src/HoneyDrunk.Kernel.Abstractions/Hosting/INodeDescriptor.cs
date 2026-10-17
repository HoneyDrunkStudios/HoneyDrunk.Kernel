namespace HoneyDrunk.Kernel.Abstractions.Hosting;

/// <summary>
/// Describes a Node's runtime identity, capabilities, and operational metadata.
/// </summary>
/// <remarks>
/// Node descriptors provide rich metadata about what a Node is and what it does.
/// This is used for service discovery, routing, monitoring, and orchestration.
/// INodeDescriptor is richer than INodeManifest - it includes runtime state.
/// </remarks>
public interface INodeDescriptor
{
    /// <summary>
    /// Gets the Node identifier.
    /// </summary>
    string NodeId { get; }

    /// <summary>
    /// Gets the semantic version of this Node.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the human-readable name of this Node.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of this Node's purpose.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the sector this Node belongs to.
    /// Sectors group related Nodes (e.g., "payments", "notifications", "analytics").
    /// </summary>
    string? Sector { get; }

    /// <summary>
    /// Gets the cluster this Node belongs to.
    /// Clusters are deployment groups within a sector (e.g., "us-east", "eu-west").
    /// </summary>
    string? Cluster { get; }

    /// <summary>
    /// Gets the capabilities this Node provides.
    /// </summary>
    IReadOnlyList<INodeCapability> Capabilities { get; }

    /// <summary>
    /// Gets the Node IDs or capability names this Node depends on.
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Gets the deployment slots this Node occupies.
    /// Slots enable blue-green deployments and A/B testing.
    /// </summary>
    IReadOnlyList<string> Slots { get; }

    /// <summary>
    /// Gets Node-specific tags for routing and filtering.
    /// </summary>
    IReadOnlyDictionary<string, string> Tags { get; }

    /// <summary>
    /// Gets the manifest this descriptor was created from (if any).
    /// </summary>
    INodeManifest? Manifest { get; }

    /// <summary>
    /// Determines if this Node has a specific capability.
    /// </summary>
    /// <param name="capabilityName">The capability name to check.</param>
    /// <returns>True if the Node has the capability; otherwise false.</returns>
    bool HasCapability(string capabilityName);
}
