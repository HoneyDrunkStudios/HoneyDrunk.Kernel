namespace HoneyDrunk.Kernel.Abstractions.Tenancy;

/// <summary>
/// Emits tenant-scoped billing or metering events after consumed work.
/// </summary>
public interface IBillingEventEmitter
{
    /// <summary>
    /// Emits a billing event.
    /// </summary>
    /// <param name="billingEvent">The billing event to emit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous emit operation.</returns>
    ValueTask EmitAsync(BillingEvent billingEvent, CancellationToken cancellationToken);
}
