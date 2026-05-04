using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Abstractions.Tenancy;

namespace HoneyDrunk.Kernel.Tenancy;

/// <summary>
/// Default tenant rate-limit policy for internal Grid usage and tests.
/// </summary>
public sealed class NoopTenantRateLimitPolicy : ITenantRateLimitPolicy
{
    private static readonly TenantRateLimitDecision AllowDecision = new(
        TenantRateLimitOutcome.Allow,
        RetryAfter: null,
        Reason: null);

    /// <inheritdoc />
    public ValueTask<TenantRateLimitDecision> EvaluateAsync(
        TenantId tenantId,
        string operationKey,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKey);

        return ValueTask.FromResult(AllowDecision);
    }
}
