namespace HoneyDrunk.Kernel.Abstractions.Lifecycle;

/// <summary>
/// Defines a hook that executes during Node startup.
/// </summary>
/// <remarks>
/// IStartupHook provides extensible startup behavior for Nodes. Multiple hooks can be registered
/// and will execute in order of registration. Use cases include:
/// - Running database migrations
/// - Seeding initial data
/// - Validating configuration
/// - Warming up external connections
/// - Registering with service discovery.
/// </remarks>
public interface IStartupHook
{
    /// <summary>
    /// Gets the priority order for this hook. Lower values execute first.
    /// Default is 0. Use negative values for early execution, positive for late.
    /// </summary>
    int Priority => 0;

    /// <summary>
    /// Executes the startup hook logic.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token for the startup operation.</param>
    /// <returns>A task that completes when the hook finishes.</returns>
    Task ExecuteAsync(CancellationToken cancellationToken);
}
