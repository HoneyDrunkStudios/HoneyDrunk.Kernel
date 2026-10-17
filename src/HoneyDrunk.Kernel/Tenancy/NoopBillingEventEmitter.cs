using HoneyDrunk.Kernel.Abstractions.Tenancy;

namespace HoneyDrunk.Kernel.Tenancy;

/// <summary>
/// Default billing-event emitter for internal Grid usage and tests.
/// </summary>
public sealed class NoopBillingEventEmitter : IBillingEventEmitter
{
    /// <inheritdoc />
    public ValueTask EmitAsync(BillingEvent billingEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(billingEvent);

        return ValueTask.CompletedTask;
    }
}
