namespace HoneyDrunk.Kernel.Abstractions.Hosting;

/// <summary>
/// Defines a capability or feature that a Node provides to the Grid.
/// </summary>
/// <remarks>
/// Node capabilities enable dynamic service discovery and routing.
/// Other Nodes can discover and invoke capabilities without knowing specific Node IDs.
/// Examples: "payment-processing", "email-sending", "data-transformation".
/// </remarks>
public interface INodeCapability
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
    /// Gets the capability version (independent of Node version).
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the category/domain of this capability.
    /// Examples: "data", "messaging", "computation", "integration".
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets the protocols this capability supports.
    /// Examples: "http", "grpc", "message-queue", "graphql".
    /// </summary>
    IReadOnlyList<string> SupportedProtocols { get; }

    /// <summary>
    /// Gets capability-specific endpoints or addresses.
    /// Key: protocol name, Value: endpoint URI.
    /// </summary>
    IReadOnlyDictionary<string, string> Endpoints { get; }

    /// <summary>
    /// Gets the input schema for this capability (if applicable).
    /// Can be JSON Schema, OpenAPI spec reference, or other schema format.
    /// </summary>
    string? InputSchema { get; }

    /// <summary>
    /// Gets the output schema for this capability (if applicable).
    /// </summary>
    string? OutputSchema { get; }

    /// <summary>
    /// Gets capability-specific metadata and constraints.
    /// Examples: rate limits, SLA guarantees, cost per invocation.
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }
}
