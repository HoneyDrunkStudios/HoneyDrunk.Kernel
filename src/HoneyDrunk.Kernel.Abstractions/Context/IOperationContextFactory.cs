namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Factory for creating <see cref="IOperationContext"/> instances and setting ambient accessor state.
/// </summary>
public interface IOperationContextFactory
{
    /// <summary>
    /// Creates a new operation context for the given operation name using the current <see cref="IGridContext"/>.
    /// Sets <see cref="IOperationContextAccessor.Current"/> to the created context.
    /// </summary>
    /// <param name="operationName">Logical operation name.</param>
    /// <param name="metadata">Optional initial metadata.</param>
    /// <returns>The created operation context.</returns>
    IOperationContext Create(string operationName, IReadOnlyDictionary<string, object?>? metadata = null);
}
