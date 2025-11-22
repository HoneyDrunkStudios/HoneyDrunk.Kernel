namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Represents the context of a Node in the HoneyDrunk.OS Grid, including its identity,
/// version, lifecycle stage, and runtime metadata.
/// </summary>
/// <remarks>
/// NodeContext is a singleton per Node process and provides OS-level information about
/// the Node's identity, capabilities, and current operational state. Unlike GridContext
/// which flows per-operation, NodeContext is process-scoped and relatively static.
/// </remarks>
public interface INodeContext
{
    /// <summary>
    /// Gets the unique identifier of this Node.
    /// Example: "payment-node", "notification-node", "auth-gateway".
    /// </summary>
    string NodeId { get; }

    /// <summary>
    /// Gets the semantic version of this Node.
    /// Example: "1.2.3", "2.0.0-beta.1".
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the Studio identifier that owns this Node.
    /// Example: "honeycomb", "staging", "dev-alice".
    /// </summary>
    string StudioId { get; }

    /// <summary>
    /// Gets the environment in which this Node is running.
    /// Example: "production", "staging", "development".
    /// </summary>
    string Environment { get; }

    /// <summary>
    /// Gets the current lifecycle stage of this Node.
    /// </summary>
    NodeLifecycleStage LifecycleStage { get; }

    /// <summary>
    /// Gets the UTC timestamp when this Node started.
    /// </summary>
    DateTimeOffset StartedAtUtc { get; }

    /// <summary>
    /// Gets the machine/host name where this Node is running.
    /// </summary>
    string MachineName { get; }

    /// <summary>
    /// Gets the process ID of this Node.
    /// </summary>
    int ProcessId { get; }

    /// <summary>
    /// Gets optional tags/labels associated with this Node instance.
    /// Used for routing, capability advertisement, and observability.
    /// </summary>
    IReadOnlyDictionary<string, string> Tags { get; }

    /// <summary>
    /// Updates the lifecycle stage of this Node.
    /// </summary>
    /// <param name="stage">The new lifecycle stage.</param>
    void SetLifecycleStage(NodeLifecycleStage stage);
}
