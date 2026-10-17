using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace HoneyDrunk.Kernel.Context;

/// <summary>
/// Default implementation of <see cref="IOperationContextFactory"/> creating <see cref="OperationContext"/> and setting ambient accessor.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates OperationContext instances that reference the current scoped GridContext.
/// It resolves GridContext from the same DI scope to ensure all components see the same context instance.
/// </para>
/// <para>
/// <strong>Important:</strong> The GridContext is resolved from the scoped IGridContext,
/// not from an independent accessor, ensuring DI, accessor, and OperationContext always agree.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="OperationContextFactory"/> class.
/// </remarks>
/// <param name="accessor">The operation context accessor.</param>
/// <param name="loggerFactory">The logger factory.</param>
/// <param name="gridContext">The scoped GridContext (same instance as DI and accessor).</param>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Registered and instantiated via DI in HoneyDrunkNodeServiceCollectionExtensions.")]
internal sealed class OperationContextFactory(IOperationContextAccessor accessor, ILoggerFactory loggerFactory, IGridContext gridContext) : IOperationContextFactory
{
    private readonly IOperationContextAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    private readonly IGridContext _gridContext = gridContext ?? throw new ArgumentNullException(nameof(gridContext));

    /// <inheritdoc />
    public IOperationContext Create(string operationName, IReadOnlyDictionary<string, object?>? metadata = null)
    {
        // GridContext must be initialized before creating operations
        if (!_gridContext.IsInitialized)
        {
            throw new InvalidOperationException(
                "Cannot create OperationContext: GridContext has not been initialized. " +
                "Ensure UseGridContext() middleware is registered and runs before this code, " +
                "or initialize the GridContext explicitly for background operations.");
        }

        var logger = _loggerFactory.CreateLogger<OperationContext>();
        var operationId = Ulid.NewUlid().ToString();
        var ctx = new OperationContext(_gridContext, operationName, operationId, logger, metadata);
        _accessor.Current = ctx;
        return ctx;
    }
}
