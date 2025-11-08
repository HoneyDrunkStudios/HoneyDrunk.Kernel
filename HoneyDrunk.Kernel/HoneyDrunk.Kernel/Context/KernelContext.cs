using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Context;

/// <summary>
/// Default implementation of kernel context with primary constructor.
/// </summary>
/// <param name="correlationId">The correlation identifier.</param>
/// <param name="causationId">The causation identifier.</param>
/// <param name="baggage">The context baggage.</param>
/// <param name="cancellation">The cancellation token.</param>
public sealed class KernelContext(
    string correlationId,
    string? causationId,
    IReadOnlyDictionary<string, string> baggage,
    CancellationToken cancellation) : IKernelContext
{
    /// <inheritdoc />
    public string CorrelationId { get; } = correlationId;

    /// <inheritdoc />
    public string? CausationId { get; } = causationId;

    /// <inheritdoc />
    public CancellationToken Cancellation { get; } = cancellation;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Baggage { get; } = baggage;

    /// <inheritdoc />
    public IDisposable BeginScope() => new NoOpDisposable();

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
