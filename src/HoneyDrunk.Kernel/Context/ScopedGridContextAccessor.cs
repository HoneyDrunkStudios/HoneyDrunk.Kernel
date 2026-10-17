// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Kernel.Context;

/// <summary>
/// AsyncLocal-based accessor for non-HTTP scenarios (background services, jobs).
/// </summary>
/// <remarks>
/// This accessor maintains a reference to the current scope's GridContext via AsyncLocal.
/// It is used when HTTP context is not available. The context must be explicitly set
/// via <see cref="SetContext"/> at the start of background operations.
/// </remarks>
public sealed class ScopedGridContextAccessor : IGridContextAccessor
{
    private static readonly AsyncLocal<IGridContext?> CurrentContext = new();

    /// <inheritdoc />
    public IGridContext GridContext
    {
        get
        {
            var context = CurrentContext.Value ?? throw new InvalidOperationException(
                    "GridContext has not been set for this execution context. " +
                    "For background services, call SetContext() at the start of the operation. " +
                    "For HTTP requests, use GridContextAccessor instead.");
            return context;
        }
    }

    /// <summary>
    /// Sets the GridContext for the current async execution flow.
    /// </summary>
    /// <param name="context">The context to set.</param>
    /// <returns>A disposable that clears the context when disposed.</returns>
    public static IDisposable SetContext(IGridContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        CurrentContext.Value = context;
        return new ContextScope();
    }

    /// <summary>
    /// Clears the current context. Called when scope ends.
    /// </summary>
    internal static void ClearContext()
    {
        CurrentContext.Value = null;
    }

    private sealed class ContextScope : IDisposable
    {
        public void Dispose() => ClearContext();
    }
}
