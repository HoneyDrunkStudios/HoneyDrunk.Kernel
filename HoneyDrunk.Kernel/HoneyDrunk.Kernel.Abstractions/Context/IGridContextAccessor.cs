namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Provides ambient access to the current DI-scoped Grid context.
/// </summary>
/// <remarks>
/// <para>
/// IGridContextAccessor provides access to the same GridContext instance that DI resolves.
/// It is NOT an independent store - it mirrors the scoped GridContext owned by DI.
/// </para>
/// <para>
/// <strong>Invariant:</strong> <c>accessor.GridContext</c> and <c>serviceProvider.GetRequiredService&lt;IGridContext&gt;()</c>
/// always return the same object instance within a scope. If they diverge, it is a bug.
/// </para>
/// <para>
/// Use sparingly - prefer explicit <c>IGridContext</c> injection via constructor when possible.
/// The accessor exists for scenarios where constructor injection is not feasible (e.g., static methods,
/// cross-cutting concerns).
/// </para>
/// </remarks>
public interface IGridContextAccessor
{
    /// <summary>
    /// Gets the current Grid context for the active scope.
    /// </summary>
    /// <remarks>
    /// Throws <see cref="InvalidOperationException"/> if accessed outside a valid scope
    /// or if Kernel setup is incomplete. Never returns null in valid configurations.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no scope is active or Kernel is not properly configured.
    /// </exception>
    IGridContext GridContext { get; }
}
