using HoneyDrunk.Kernel.Abstractions.Context;
using System.Diagnostics.CodeAnalysis;

namespace HoneyDrunk.Kernel.Context;

/// <summary>
/// AsyncLocal-backed implementation of <see cref="IOperationContextAccessor"/> for ambient operation propagation.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Registered and instantiated via DI in HoneyDrunkNodeServiceCollectionExtensions.")]
internal sealed class OperationContextAccessor : IOperationContextAccessor
{
    private static readonly AsyncLocal<IOperationContext?> CurrentContext = new();

    public IOperationContext? Current
    {
        get => CurrentContext.Value;
        set => CurrentContext.Value = value;
    }
}
