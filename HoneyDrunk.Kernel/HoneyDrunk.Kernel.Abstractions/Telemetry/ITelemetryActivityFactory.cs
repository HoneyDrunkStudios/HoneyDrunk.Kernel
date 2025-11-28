using HoneyDrunk.Kernel.Abstractions.Context;
using System.Diagnostics;

namespace HoneyDrunk.Kernel.Abstractions.Telemetry;

/// <summary>
/// Factory abstraction for creating enriched <see cref="Activity"/> instances using ambient Grid / Operation context.
/// </summary>
public interface ITelemetryActivityFactory
{
    /// <summary>
    /// Starts an <see cref="Activity"/> with standardized HoneyDrunk tags plus optional caller-supplied tags.
    /// </summary>
    /// <param name="name">Logical activity name.</param>
    /// <param name="additionalTags">Optional extra tags (override defaults on key collision).</param>
    /// <returns>The started <see cref="Activity"/> or null if not sampled.</returns>
    Activity? Start(string name, IReadOnlyDictionary<string, object?>? additionalTags = null);

    /// <summary>
    /// Starts an <see cref="Activity"/> for an explicit <see cref="IGridContext"/> (bypassing ambient accessor) plus optional operation context.
    /// </summary>
    /// <param name="name">Logical activity name.</param>
    /// <param name="gridContext">Explicit grid context.</param>
    /// <param name="operationContext">Optional operation context for outcome tagging.</param>
    /// <param name="additionalTags">Optional extra tags.</param>
    /// <returns>The started <see cref="Activity"/> or null if not sampled.</returns>
    Activity? StartExplicit(string name, IGridContext gridContext, IOperationContext? operationContext = null, IReadOnlyDictionary<string, object?>? additionalTags = null);
}
