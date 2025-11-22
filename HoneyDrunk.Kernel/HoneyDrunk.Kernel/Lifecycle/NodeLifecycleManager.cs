using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Health;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Kernel.Lifecycle;

/// <summary>
/// Orchestrates Node lifecycle and coordinates health/readiness checks.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NodeLifecycleManager"/> class.
/// </remarks>
/// <param name="nodeContext">The Node context.</param>
/// <param name="healthContributors">The health contributors.</param>
/// <param name="readinessContributors">The readiness contributors.</param>
/// <param name="logger">The logger instance.</param>
public sealed class NodeLifecycleManager(
    INodeContext nodeContext,
    IEnumerable<IHealthContributor> healthContributors,
    IEnumerable<IReadinessContributor> readinessContributors,
    ILogger<NodeLifecycleManager> logger)
{
    private readonly INodeContext _nodeContext = nodeContext ?? throw new ArgumentNullException(nameof(nodeContext));
    private readonly IEnumerable<IHealthContributor> _healthContributors = healthContributors ?? throw new ArgumentNullException(nameof(healthContributors));
    private readonly IEnumerable<IReadinessContributor> _readinessContributors = readinessContributors ?? throw new ArgumentNullException(nameof(readinessContributors));
    private readonly ILogger<NodeLifecycleManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Performs comprehensive health check across all contributors.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregated health status and details from all contributors.</returns>
    public async Task<(HealthStatus status, IReadOnlyDictionary<string, (HealthStatus status, string? message)> details)>
        CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var contributors = _healthContributors.OrderBy(c => c.Priority).ToList();

        if (contributors.Count == 0)
        {
            return (status: HealthStatus.Healthy, new Dictionary<string, (HealthStatus, string?)>());
        }

        var details = new Dictionary<string, (HealthStatus status, string? message)>();
        var worstStatus = HealthStatus.Healthy;

        foreach (var contributor in contributors)
        {
            try
            {
                var (status, message) = await contributor.CheckHealthAsync(cancellationToken);
                details[contributor.Name] = (status, message);

                // Track worst status
                if (status > worstStatus)
                {
                    worstStatus = status;
                }

                // If critical contributor is unhealthy, fail fast
                if (contributor.IsCritical && status == HealthStatus.Unhealthy)
                {
                    _logger.LogError(
                        "Critical health contributor {ContributorName} is unhealthy: {Message}",
                        contributor.Name,
                        message);

                    return (status: HealthStatus.Unhealthy, details);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Health contributor {ContributorName} threw exception", contributor.Name);
                details[contributor.Name] = (status: HealthStatus.Unhealthy, $"Exception: {ex.Message}");

                if (contributor.IsCritical)
                {
                    return (status: HealthStatus.Unhealthy, details);
                }

                worstStatus = HealthStatus.Unhealthy;
            }
        }

        return (worstStatus, details);
    }

    /// <summary>
    /// Performs comprehensive readiness check across all contributors.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregated readiness status and details from all contributors.</returns>
    public async Task<(bool isReady, IReadOnlyDictionary<string, (bool isReady, string? reason)> details)>
        CheckReadinessAsync(CancellationToken cancellationToken = default)
    {
        var contributors = _readinessContributors.OrderBy(c => c.Priority).ToList();

        if (contributors.Count == 0)
        {
            return (true, new Dictionary<string, (bool, string?)>());
        }

        var details = new Dictionary<string, (bool isReady, string? reason)>();
        var allReady = true;

        foreach (var contributor in contributors)
        {
            try
            {
                var (isReady, reason) = await contributor.CheckReadinessAsync(cancellationToken);
                details[contributor.Name] = (isReady, reason);

                if (!isReady)
                {
                    if (contributor.IsRequired)
                    {
                        _logger.LogWarning(
                            "Required readiness contributor {ContributorName} is not ready: {Reason}",
                            contributor.Name,
                            reason);
                        allReady = false;
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Optional readiness contributor {ContributorName} is not ready: {Reason}",
                            contributor.Name,
                            reason);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Readiness contributor {ContributorName} threw exception", contributor.Name);
                details[contributor.Name] = (false, $"Exception: {ex.Message}");

                if (contributor.IsRequired)
                {
                    allReady = false;
                }
            }
        }

        return (allReady, details);
    }

    /// <summary>
    /// Updates the Node lifecycle stage and logs the transition.
    /// </summary>
    /// <param name="newStage">The new lifecycle stage.</param>
    public void TransitionToStage(NodeLifecycleStage newStage)
    {
        var currentStage = _nodeContext.LifecycleStage;

        if (currentStage == newStage)
        {
            return;
        }

        _logger.LogInformation(
            "Node {NodeId} transitioning from {CurrentStage} to {NewStage}",
            _nodeContext.NodeId,
            currentStage,
            newStage);

        _nodeContext.SetLifecycleStage(newStage);
    }
}
