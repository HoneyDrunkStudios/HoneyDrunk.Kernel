using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions.Tenancy;

/// <summary>
/// Evaluates whether tenant-scoped work may be admitted into a Node intake pipeline.
/// </summary>
public interface ITenantRateLimitPolicy
{
    /// <summary>
    /// Evaluates the rate-limit decision for a tenant and operation.
    /// </summary>
    /// <param name="tenantId">The tenant requesting work.</param>
    /// <param name="operationKey">The stable operation key, such as email or sms.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant rate-limit decision.</returns>
    ValueTask<TenantRateLimitDecision> EvaluateAsync(
        TenantId tenantId,
        string operationKey,
        CancellationToken cancellationToken);
}
