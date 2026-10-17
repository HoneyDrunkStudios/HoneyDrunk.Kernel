namespace HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Ambient accessor for the current <see cref="IOperationContext"/> flowing through async boundaries.
/// </summary>
/// <remarks>
/// Similar to IHttpContextAccessor but for Grid operations. Prefer explicit passing of contexts where practical.
/// Use only in infrastructure glue and legacy adaptation layers.
/// </remarks>
public interface IOperationContextAccessor
{
    /// <summary>
    /// Gets or sets the current operation context; setting to <c>null</c> clears the ambient context.
    /// </summary>
    IOperationContext? Current { get; set; }
}
