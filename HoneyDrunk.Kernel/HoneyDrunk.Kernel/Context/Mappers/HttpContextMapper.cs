using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.AspNetCore.Http;

namespace HoneyDrunk.Kernel.Context.Mappers;

/// <summary>
/// Maps HTTP request context to GridContext.
/// </summary>
/// <remarks>
/// Extracts correlation/causation IDs from HTTP headers and creates GridContext.
/// Standard headers:
/// - X-Correlation-Id or traceparent.
/// - X-Causation-Id.
/// - X-Studio-Id.
/// </remarks>
public sealed class HttpContextMapper
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string CausationIdHeader = "X-Causation-Id";
    private const string StudioIdHeader = "X-Studio-Id";
    private const string TraceParentHeader = "traceparent";

    private readonly string _nodeId;
    private readonly string _defaultStudioId;
    private readonly string _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContextMapper"/> class.
    /// </summary>
    /// <param name="nodeId">The Node identifier.</param>
    /// <param name="defaultStudioId">The default Studio identifier.</param>
    /// <param name="environment">The environment name.</param>
    public HttpContextMapper(string nodeId, string defaultStudioId, string environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId, nameof(nodeId));
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultStudioId, nameof(defaultStudioId));
        ArgumentException.ThrowIfNullOrWhiteSpace(environment, nameof(environment));

        _nodeId = nodeId;
        _defaultStudioId = defaultStudioId;
        _environment = environment;
    }

    /// <summary>
    /// Creates a GridContext from an HTTP context.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>A GridContext populated from HTTP headers.</returns>
    public IGridContext MapFromHttpContext(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext, nameof(httpContext));

        var correlationId = ExtractCorrelationId(httpContext);
        var causationId = ExtractHeader(httpContext, CausationIdHeader);
        var studioId = ExtractHeader(httpContext, StudioIdHeader) ?? _defaultStudioId;

        var baggage = ExtractBaggage(httpContext);

        return new GridContext(
            correlationId: correlationId,
            nodeId: _nodeId,
            studioId: studioId,
            environment: _environment,
            causationId: causationId,
            baggage: baggage,
            cancellation: httpContext.RequestAborted);
    }

    private static string ExtractCorrelationId(HttpContext httpContext)
    {
        // Try X-Correlation-Id header first
        var correlationId = ExtractHeader(httpContext, CorrelationIdHeader);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        // Fall back to W3C traceparent
        var traceParent = ExtractHeader(httpContext, TraceParentHeader);
        if (!string.IsNullOrWhiteSpace(traceParent))
        {
            // traceparent format: version-traceid-spanid-flags
            var parts = traceParent.Split('-');
            if (parts.Length >= 2)
            {
                return parts[1]; // Extract trace ID
            }
        }

        // Generate new correlation ID
        return Ulid.NewUlid().ToString();
    }

    private static string? ExtractHeader(HttpContext httpContext, string headerName)
    {
        if (httpContext.Request.Headers.TryGetValue(headerName, out var values))
        {
            return values.FirstOrDefault();
        }

        return null;
    }

    private static Dictionary<string, string> ExtractBaggage(HttpContext httpContext)
    {
        var baggage = new Dictionary<string, string>();

        // Extract from baggage header (W3C format)
        if (httpContext.Request.Headers.TryGetValue("baggage", out var baggageHeader))
        {
            var baggageString = baggageHeader.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(baggageString))
            {
                foreach (var pair in baggageString.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var keyValue = pair.Split('=', 2);
                    if (keyValue.Length == 2)
                    {
                        baggage[keyValue[0].Trim()] = Uri.UnescapeDataString(keyValue[1].Trim());
                    }
                }
            }
        }

        return baggage;
    }
}
