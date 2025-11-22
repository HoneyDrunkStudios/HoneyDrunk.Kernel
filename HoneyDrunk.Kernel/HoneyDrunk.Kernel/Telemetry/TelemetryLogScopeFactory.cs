using HoneyDrunk.Kernel.Abstractions.Telemetry;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Kernel.Telemetry;

/// <summary>
/// Creates logging scopes enriched with telemetry context.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TelemetryLogScopeFactory"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
public sealed class TelemetryLogScopeFactory(ILogger logger) : ILogScopeFactory
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public IDisposable CreateScope(ITelemetryContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var properties = BuildScopeProperties(context);
        return _logger.BeginScope(properties)!;
    }

    /// <inheritdoc />
    public IDisposable CreateScope(ITelemetryContext context, IReadOnlyDictionary<string, object?> additionalProperties)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(additionalProperties, nameof(additionalProperties));

        var properties = BuildScopeProperties(context);

        // Merge additional properties
        foreach (var (key, value) in additionalProperties)
        {
            properties[key] = value;
        }

        return _logger.BeginScope(properties)!;
    }

    private static Dictionary<string, object?> BuildScopeProperties(ITelemetryContext context)
    {
        var gridContext = context.GridContext;

        var properties = new Dictionary<string, object?>
        {
            [TelemetryTags.CorrelationId] = gridContext.CorrelationId,
            [TelemetryTags.NodeId] = gridContext.NodeId,
            [TelemetryTags.StudioId] = gridContext.StudioId,
            [TelemetryTags.Environment] = gridContext.Environment,
            ["TraceId"] = context.TraceId,
            ["SpanId"] = context.SpanId,
        };

        if (gridContext.CausationId != null)
        {
            properties[TelemetryTags.CausationId] = gridContext.CausationId;
        }

        if (context.ParentSpanId != null)
        {
            properties["ParentSpanId"] = context.ParentSpanId;
        }

        return properties;
    }
}
