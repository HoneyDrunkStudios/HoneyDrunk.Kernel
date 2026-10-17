namespace HoneyDrunk.Kernel.Abstractions.Tenancy;

/// <summary>
/// Outcome returned by a tenant rate-limit policy.
/// </summary>
public enum TenantRateLimitOutcome
{
    /// <summary>Allow the operation to proceed.</summary>
    Allow,

    /// <summary>Soft throttle the operation with an optional retry advisory.</summary>
    Throttle,

    /// <summary>Reject the operation.</summary>
    Reject,
}
