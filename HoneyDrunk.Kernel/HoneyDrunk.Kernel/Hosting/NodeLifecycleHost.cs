using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Kernel.Hosting;

/// <summary>
/// Hosted service that coordinates Node lifecycle and executes startup/shutdown hooks.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated via AddHostedService<> in HoneyDrunkServiceCollectionExtensions")]
internal sealed class NodeLifecycleHost(
    INodeContext nodeContext,
    IEnumerable<INodeLifecycle> lifecycles,
    IEnumerable<IStartupHook> startupHooks,
    IEnumerable<IShutdownHook> shutdownHooks,
    ILogger<NodeLifecycleHost> logger) : IHostedService
{
    private readonly INodeContext _nodeContext = nodeContext;
    private readonly IEnumerable<INodeLifecycle> _lifecycles = lifecycles;
    private readonly IEnumerable<IStartupHook> _startupHooks = startupHooks;
    private readonly IEnumerable<IShutdownHook> _shutdownHooks = shutdownHooks;
    private readonly ILogger<NodeLifecycleHost> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting Node {NodeId} v{Version} in {Environment} environment (Studio: {StudioId})",
            _nodeContext.NodeId,
            _nodeContext.Version,
            _nodeContext.Environment,
            _nodeContext.StudioId);

        _nodeContext.SetLifecycleStage(NodeLifecycleStage.Starting);

        try
        {
            var orderedHooks = _startupHooks.OrderBy(h => h.Priority).ToList();
            foreach (var hook in orderedHooks)
            {
                var hookType = hook.GetType().Name;
                _logger.LogDebug("Executing startup hook: {HookType}", hookType);
                await hook.ExecuteAsync(cancellationToken);
            }

            foreach (var lifecycle in _lifecycles)
            {
                await lifecycle.StartAsync(cancellationToken);
            }

            _nodeContext.SetLifecycleStage(NodeLifecycleStage.Ready);
            _logger.LogInformation("Node {NodeId} is now ready", _nodeContext.NodeId);
        }
        catch (Exception ex)
        {
            _nodeContext.SetLifecycleStage(NodeLifecycleStage.Failed);
            _logger.LogCritical(ex, "Node {NodeId} failed to start", _nodeContext.NodeId);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Node {NodeId}", _nodeContext.NodeId);
        _nodeContext.SetLifecycleStage(NodeLifecycleStage.Stopping);

        try
        {
            foreach (var lifecycle in _lifecycles)
            {
                await lifecycle.StopAsync(cancellationToken);
            }

            var orderedHooks = _shutdownHooks.OrderBy(h => h.Priority).ToList();
            foreach (var hook in orderedHooks)
            {
                var hookType = hook.GetType().Name;
                _logger.LogDebug("Executing shutdown hook: {HookType}", hookType);
                await hook.ExecuteAsync(cancellationToken);
            }

            _nodeContext.SetLifecycleStage(NodeLifecycleStage.Stopped);
            _logger.LogInformation("Node {NodeId} stopped successfully", _nodeContext.NodeId);
        }
        catch (Exception ex)
        {
            _nodeContext.SetLifecycleStage(NodeLifecycleStage.Failed);
            _logger.LogError(ex, "Error during Node {NodeId} shutdown", _nodeContext.NodeId);
            throw;
        }
    }
}
