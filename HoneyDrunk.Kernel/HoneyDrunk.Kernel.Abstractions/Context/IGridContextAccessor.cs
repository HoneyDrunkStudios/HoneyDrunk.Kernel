namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Provides access to the current Grid context in a given scope.
/// </summary>
/// <remarks>
/// IGridContextAccessor enables ambient context access without explicit passing.
/// Similar to IHttpContextAccessor in ASP.NET Core, this uses AsyncLocal storage
/// to flow context through async boundaries without manual propagation.
/// Use sparingly - prefer explicit context passing when possible.
/// </remarks>
public interface IGridContextAccessor
{
    /// <summary>
    /// Gets or sets the current Grid context.
    /// Returns null if no context is available in the current scope.
    /// </summary>
    IGridContext? GridContext { get; set; }
}
