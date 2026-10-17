namespace HoneyDrunk.Kernel.Abstractions.Lifecycle;

/// <summary>
/// Defines a hook that executes during Node shutdown.
/// </summary>
/// <remarks>
/// IShutdownHook provides extensible shutdown behavior for Nodes. Multiple hooks can be registered
/// and will execute in reverse order of registration (last registered runs first). Use cases include:
/// - Draining connection pools
/// - Flushing message queues
/// - Deregistering from service discovery
/// - Closing file handles
/// - Notifying dependent services.
/// </remarks>
public interface IShutdownHook
{
    /// <summary>
    /// Gets the priority order for this hook. Lower values execute first during shutdown.
    /// Default is 0. Use negative values for early shutdown, positive for late.
    /// </summary>
    int Priority => 0;

    /// <summary>
    /// Executes the shutdown hook logic.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token for the shutdown operation.</param>
    /// <returns>A task that completes when the hook finishes.</returns>
    Task ExecuteAsync(CancellationToken cancellationToken);
}
