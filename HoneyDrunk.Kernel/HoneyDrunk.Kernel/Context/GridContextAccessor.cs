using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Context;

/// <summary>
/// Default implementation of IGridContextAccessor using AsyncLocal storage.
/// </summary>
public sealed class GridContextAccessor : IGridContextAccessor
{
    private static readonly AsyncLocal<IGridContext?> _currentContext = new();

    /// <inheritdoc />
    public IGridContext? GridContext
    {
        get => _currentContext.Value;
        set => _currentContext.Value = value;
    }
}
