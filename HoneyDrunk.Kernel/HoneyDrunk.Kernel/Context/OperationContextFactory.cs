using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace HoneyDrunk.Kernel.Context;

/// <summary>
/// Default implementation of <see cref="IOperationContextFactory"/> creating <see cref="OperationContext"/> and setting ambient accessor.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Registered and instantiated via DI in HoneyDrunkNodeServiceCollectionExtensions.")]
internal sealed class OperationContextFactory(IOperationContextAccessor accessor, ILoggerFactory loggerFactory, IGridContext gridContext) : IOperationContextFactory
{
    private readonly IOperationContextAccessor _accessor = accessor;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IGridContext _gridContext = gridContext;

    public IOperationContext Create(string operationName, IReadOnlyDictionary<string, object?>? metadata = null)
    {
        var logger = _loggerFactory.CreateLogger<OperationContext>();
        var ctx = new OperationContext(_gridContext, operationName, logger, metadata);
        _accessor.Current = ctx;
        return ctx;
    }
}
