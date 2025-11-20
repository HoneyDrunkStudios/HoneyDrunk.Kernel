namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Represents the lifecycle stage of a Node in the HoneyDrunk.OS Grid.
/// </summary>
public enum NodeLifecycleStage
{
    /// <summary>
    /// Node is initializing and not yet ready to accept work.
    /// </summary>
    Initializing = 0,

    /// <summary>
    /// Node has completed initialization and is starting up services.
    /// </summary>
    Starting = 1,

    /// <summary>
    /// Node is fully operational and accepting work.
    /// </summary>
    Running = 2,

    /// <summary>
    /// Node is degraded but still operational (e.g., dependency issues).
    /// </summary>
    Degraded = 3,

    /// <summary>
    /// Node is shutting down gracefully, draining in-flight work.
    /// </summary>
    Stopping = 4,

    /// <summary>
    /// Node has shut down and is no longer accepting work.
    /// </summary>
    Stopped = 5,

    /// <summary>
    /// Node encountered a fatal error and is in a failed state.
    /// </summary>
    Failed = 6,
}
