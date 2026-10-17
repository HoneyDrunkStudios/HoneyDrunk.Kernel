using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Context;

/// <summary>
/// Default implementation of INodeContext for Node-level metadata.
/// </summary>
public sealed class NodeContext : INodeContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeContext"/> class.
    /// </summary>
    /// <param name="nodeId">The Node identifier.</param>
    /// <param name="version">The Node version.</param>
    /// <param name="studioId">The Studio identifier.</param>
    /// <param name="environment">The environment name.</param>
    /// <param name="tags">Optional tags/labels for this Node.</param>
    public NodeContext(
        string nodeId,
        string version,
        string studioId,
        string environment,
        IReadOnlyDictionary<string, string>? tags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(studioId);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        NodeId = nodeId;
        Version = version;
        StudioId = studioId;
        Environment = environment;
        Tags = tags ?? new Dictionary<string, string>();
        StartedAtUtc = DateTimeOffset.UtcNow;
        MachineName = System.Environment.MachineName;
        ProcessId = System.Environment.ProcessId;
        LifecycleStage = NodeLifecycleStage.Initializing;
    }

    /// <inheritdoc />
    public string NodeId { get; }

    /// <inheritdoc />
    public string Version { get; }

    /// <inheritdoc />
    public string StudioId { get; }

    /// <inheritdoc />
    public string Environment { get; }

    /// <inheritdoc />
    public NodeLifecycleStage LifecycleStage { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset StartedAtUtc { get; }

    /// <inheritdoc />
    public string MachineName { get; }

    /// <inheritdoc />
    public int ProcessId { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Tags { get; }

    /// <inheritdoc />
    public void SetLifecycleStage(NodeLifecycleStage stage)
    {
        LifecycleStage = stage;
    }
}
