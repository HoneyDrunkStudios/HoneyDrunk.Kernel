namespace HoneyDrunk.Kernel.Abstractions.Tenancy;

/// <summary>
/// Decision returned by <see cref="ITenantRateLimitPolicy"/>.
/// </summary>
/// <param name="Outcome">The rate-limit outcome.</param>
/// <param name="RetryAfter">Optional retry advisory for throttled or rejected requests.</param>
/// <param name="Reason">Non-PII reason suitable for an error envelope.</param>
public sealed record TenantRateLimitDecision(
    TenantRateLimitOutcome Outcome,
    TimeSpan? RetryAfter,
    string? Reason);
