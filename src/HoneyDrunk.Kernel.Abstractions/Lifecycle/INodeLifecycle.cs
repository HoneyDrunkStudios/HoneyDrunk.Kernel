namespace HoneyDrunk.Kernel.Abstractions.Lifecycle;

/// <summary>
/// Defines lifecycle hooks for Node startup and shutdown coordination.
/// </summary>
/// <remarks>
/// INodeLifecycle provides OS-level hooks for coordinating Node initialization and teardown.
/// Implementations should handle Node-specific initialization logic such as:
/// - Database connection warmup
/// - Cache preloading
/// - Health check registration
/// - Background service startup
/// - Graceful shutdown and resource cleanup.
/// </remarks>
public interface INodeLifecycle
{
    /// <summary>
    /// Called during Node startup, before the Node begins accepting work.
    /// Use this to initialize resources, warm caches, or perform health checks.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token for the startup operation.</param>
    /// <returns>A task that completes when startup is finished.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Called during Node shutdown, after the Node stops accepting new work.
    /// Use this to drain in-flight requests, flush buffers, and release resources.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token for the shutdown operation.</param>
    /// <returns>A task that completes when shutdown is finished.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
