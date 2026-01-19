using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.AspNetCore.Http;

namespace HoneyDrunk.Kernel.Context.Mappers;

/// <summary>
/// Maps HTTP request headers to GridContext initialization parameters.
/// </summary>
/// <remarks>
/// <para>
/// This mapper extracts correlation, causation, tenant, project, and baggage values from HTTP headers.
/// It is used by middleware to initialize the scoped GridContext.
/// </para>
/// <para>
/// Standard headers:
/// </para>
/// <list type="bullet">
/// <item>X-Correlation-Id or traceparent → CorrelationId</item>
/// <item>X-Causation-Id → CausationId</item>
/// <item>X-Tenant-Id → TenantId (identity only)</item>
/// <item>X-Project-Id → ProjectId (identity only)</item>
/// <item>baggage, X-Baggage-* → Baggage</item>
/// </list>
/// </remarks>
public sealed class HttpContextMapper
{
    /// <summary>
    /// Extracts GridContext initialization values from an HTTP request.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>Values needed to initialize a GridContext.</returns>
    public static GridContextInitValues ExtractFromHttpContext(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var correlationId = ExtractCorrelationId(httpContext);
        var causationId = ExtractHeader(httpContext, GridHeaderNames.CausationId);
        var tenantId = ExtractHeader(httpContext, GridHeaderNames.TenantId);
        var projectId = ExtractHeader(httpContext, GridHeaderNames.ProjectId);
        var baggage = ExtractBaggage(httpContext);

        return new GridContextInitValues(
            correlationId,
            causationId,
            tenantId,
            projectId,
            baggage,
            httpContext.RequestAborted);
    }

    /// <summary>
    /// Initializes an existing GridContext from HTTP request headers.
    /// </summary>
    /// <param name="context">The GridContext to initialize.</param>
    /// <param name="httpContext">The HTTP context containing headers.</param>
    public static void InitializeFromHttpContext(GridContext context, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(httpContext);

        var values = ExtractFromHttpContext(httpContext);
        context.Initialize(
            values.CorrelationId,
            values.CausationId,
            values.TenantId,
            values.ProjectId,
            values.Baggage,
            values.Cancellation);
    }

    private static string ExtractCorrelationId(HttpContext httpContext)
    {
        // Try X-Correlation-Id header first
        var correlationId = ExtractHeader(httpContext, GridHeaderNames.CorrelationId);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        // Fall back to W3C traceparent
        var traceParent = ExtractHeader(httpContext, GridHeaderNames.TraceParent);
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

        // Extract from W3C baggage header (baggage: key1=value1;metadata,key2=value2)
        if (httpContext.Request.Headers.TryGetValue(GridHeaderNames.Baggage, out var baggageHeader))
        {
            var baggageString = baggageHeader.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(baggageString))
            {
                var pairs = baggageString
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(ParseBaggageItem)
                    .Where(item => item.HasValue)
                    .Select(item => item!.Value);

                foreach (var (key, value) in pairs)
                {
                    baggage[key] = value;
                }
            }
        }

        // Extract from X-Baggage-* headers (X-Baggage-tenant-id: value)
        foreach (var header in httpContext.Request.Headers)
        {
            if (header.Key.StartsWith(GridHeaderNames.BaggagePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var key = header.Key[GridHeaderNames.BaggagePrefix.Length..]; // Remove prefix
                var value = header.Value.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    baggage[key] = value;
                }
            }
        }

        return baggage;
    }

    private static (string key, string value)? ParseBaggageItem(string item)
    {
        // Split on semicolon to separate key=value from properties/metadata
        var parts = item.Split(';', StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        // Parse the key=value part (ignore properties for now)
        var keyValue = parts[0].Split('=', 2, StringSplitOptions.TrimEntries);
        if (keyValue.Length != 2 || string.IsNullOrWhiteSpace(keyValue[0]) || string.IsNullOrWhiteSpace(keyValue[1]))
        {
            return null;
        }

        var key = keyValue[0].Trim();
        var value = Uri.UnescapeDataString(keyValue[1].Trim());

        return (key, value);
    }
}
