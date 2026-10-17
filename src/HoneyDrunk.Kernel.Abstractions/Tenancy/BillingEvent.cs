using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions.Tenancy;

/// <summary>
/// Tenant-scoped billing or metering event emitted after consumed work.
/// </summary>
/// <param name="TenantId">The tenant that consumed capacity.</param>
/// <param name="EventType">Stable event type, such as notify.delivery.success.</param>
/// <param name="OperationKey">Stable operation key, such as email or sms.</param>
/// <param name="Units">Consumed units.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the event occurred.</param>
/// <param name="CorrelationId">Correlation identifier for traceability.</param>
/// <param name="Attributes">Bounded non-PII provider metadata.</param>
public sealed record BillingEvent(
    TenantId TenantId,
    string EventType,
    string OperationKey,
    long Units,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    IReadOnlyDictionary<string, string> Attributes);
