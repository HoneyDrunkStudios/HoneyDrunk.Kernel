using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Telemetry;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace HoneyDrunk.Kernel.Telemetry;

/// <summary>
/// Factory for starting telemetry <see cref="Activity"/> instances using ambient or explicit contexts.
/// </summary>
/// <remarks>
/// Internal because consumers use <see cref="ITelemetryActivityFactory"/>; instantiated via DI.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="TelemetryActivityFactory"/> class.
/// </remarks>
/// <param name="gridAccessor">Ambient Grid context accessor.</param>
/// <param name="opAccessor">Ambient Operation context accessor.</param>
/// <param name="enrichers">Trace enrichers applied to each activity.</param>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Registered via DI.")]
internal sealed class TelemetryActivityFactory(
    IGridContextAccessor gridAccessor,
    IOperationContextAccessor opAccessor,
    IEnumerable<ITraceEnricher> enrichers) : ITelemetryActivityFactory
{
    private readonly IGridContextAccessor _gridAccessor = gridAccessor;
    private readonly IOperationContextAccessor _opAccessor = opAccessor;
    private readonly IEnumerable<ITraceEnricher> _enrichers = enrichers;

    /// <summary>
    /// Starts an <see cref="Activity"/> using ambient contexts; returns null if no ambient GridContext.
    /// </summary>
    /// <param name="name">Activity operation name.</param>
    /// <param name="additionalTags">Optional extra tags.</param>
    /// <returns>Started activity or null.</returns>
    public Activity? Start(string name, IReadOnlyDictionary<string, object?>? additionalTags = null)
    {
        var grid = _gridAccessor.GridContext;
        if (grid is null)
        {
            return null; // No ambient context; caller can use StartExplicit instead.
        }

        var op = _opAccessor.Current;
        return HoneyDrunkTelemetry.StartActivity(name, grid, op, _enrichers, additionalTags);
    }

    /// <summary>
    /// Starts an <see cref="Activity"/> with explicitly provided contexts.
    /// </summary>
    /// <param name="name">Activity operation name.</param>
    /// <param name="gridContext">Grid context.</param>
    /// <param name="operationContext">Optional operation context.</param>
    /// <param name="additionalTags">Optional extra tags.</param>
    /// <returns>Started activity or null.</returns>
    public Activity? StartExplicit(string name, IGridContext gridContext, IOperationContext? operationContext = null, IReadOnlyDictionary<string, object?>? additionalTags = null)
    {
        ArgumentNullException.ThrowIfNull(gridContext);
        return HoneyDrunkTelemetry.StartActivity(name, gridContext, operationContext, _enrichers, additionalTags);
    }
}
