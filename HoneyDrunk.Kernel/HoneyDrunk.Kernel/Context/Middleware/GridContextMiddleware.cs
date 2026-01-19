using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Kernel.Context.Middleware;

/// <summary>
/// HTTP middleware that initializes the scoped GridContext from request headers and establishes an OperationContext.
/// </summary>
/// <remarks>
/// <para>
/// This middleware is responsible for:
/// </para>
/// <list type="number">
/// <item>Extracting correlation/causation/studio/baggage headers from the inbound request.</item>
/// <item>Initializing the DI-scoped GridContext with extracted values.</item>
/// <item>Creating an OperationContext for the request scope.</item>
/// <item>Echoing correlation and node ID headers to the response for traceability.</item>
/// <item>Marking the GridContext as disposed when the request ends.</item>
/// </list>
/// <para>
/// <strong>Important:</strong> This middleware does NOT create a new GridContext. It initializes the
/// existing scoped instance created by DI. This ensures DI, accessor, and OperationContext all see
/// the same GridContext.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="GridContextMiddleware"/> class.
/// </remarks>
/// <param name="next">The next request delegate in the pipeline.</param>
/// <param name="logger">ILogger for diagnostics.</param>
public sealed class GridContextMiddleware(RequestDelegate next, ILogger<GridContextMiddleware> logger)
{
    private const int MaxHeaderLength = 256;

    /// <summary>
    /// Invokes the middleware for the given HTTP request.
    /// </summary>
    /// <param name="httpContext">Current HTTP context.</param>
    /// <returns>A task representing the asynchronous pipeline continuation.</returns>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Resolve the scoped GridContext from DI - this is THE single instance for this request
        var gridContext = httpContext.RequestServices.GetRequiredService<GridContext>();
        var nodeContext = httpContext.RequestServices.GetRequiredService<INodeContext>();
        var opAccessor = httpContext.RequestServices.GetRequiredService<IOperationContextAccessor>();
        var opFactory = httpContext.RequestServices.GetRequiredService<IOperationContextFactory>();

        // Extract values from headers and initialize the scoped context
        var correlationId = ExtractCorrelationId(httpContext);
        var causationId = ExtractHeader(httpContext, GridHeaderNames.CausationId);
        var tenantId = ExtractHeader(httpContext, GridHeaderNames.TenantId);
        var projectId = ExtractHeader(httpContext, GridHeaderNames.ProjectId);
        var baggage = ExtractBaggage(httpContext);

        // Apply defensive truncation
        correlationId = Truncate(correlationId);
        causationId = TruncateNullable(causationId);
        tenantId = TruncateNullable(tenantId);
        projectId = TruncateNullable(projectId);

        // Initialize the scoped GridContext (this throws if already initialized)
        gridContext.Initialize(
            correlationId: correlationId,
            causationId: causationId,
            tenantId: tenantId,
            projectId: projectId,
            baggage: baggage,
            cancellation: httpContext.RequestAborted);

        // Create operation context (scope = request) with metadata
        var metadata = new Dictionary<string, object?>
        {
            ["http.method"] = httpContext.Request.Method,
            ["http.path"] = httpContext.Request.Path.ToString(),
            ["http.request_id"] = httpContext.TraceIdentifier,
        };
        var operation = opFactory.Create("HttpRequest", metadata);
        opAccessor.Current = operation;

        // Capture values for response headers BEFORE disposal
        // (OnStarting runs after finally block, so we can't access gridContext there)
        var responseCorrelationId = correlationId;
        var responseNodeId = nodeContext.NodeId;
        var responseTenantId = tenantId;
        var responseProjectId = projectId;

        // Echo correlation + node id for client trace continuity
        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers[GridHeaderNames.CorrelationId] = responseCorrelationId;
            httpContext.Response.Headers[GridHeaderNames.NodeId] = responseNodeId;

            if (!string.IsNullOrWhiteSpace(responseTenantId))
            {
                httpContext.Response.Headers[GridHeaderNames.TenantId] = responseTenantId;
            }

            if (!string.IsNullOrWhiteSpace(responseProjectId))
            {
                httpContext.Response.Headers[GridHeaderNames.ProjectId] = responseProjectId;
            }

            return Task.CompletedTask;
        });

        try
        {
            await next(httpContext);
            operation.Complete();
        }
        catch (Exception ex)
        {
            operation.Fail("Unhandled exception", ex);
            logger.LogError(ex, "Unhandled exception during request with correlation {CorrelationId}", gridContext.CorrelationId);
            throw;
        }
        finally
        {
            // Clear operation context accessor
            opAccessor.Current = null;
            operation.Dispose();

            // Mark the GridContext as disposed to catch fire-and-forget misuse
            gridContext.MarkDisposed();
        }
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
            var parts = traceParent.Split('-');
            if (parts.Length >= 2)
            {
                return parts[1];
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

        // Extract from W3C baggage header
        if (httpContext.Request.Headers.TryGetValue(GridHeaderNames.Baggage, out var baggageHeader))
        {
            var baggageString = baggageHeader.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(baggageString))
            {
                foreach (var item in baggageString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var parsed = ParseBaggageItem(item);
                    if (parsed.HasValue)
                    {
                        baggage[parsed.Value.key] = parsed.Value.value;
                    }
                }
            }
        }

        // Extract from X-Baggage-* headers
        foreach (var header in httpContext.Request.Headers)
        {
            if (header.Key.StartsWith(GridHeaderNames.BaggagePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var key = header.Key[GridHeaderNames.BaggagePrefix.Length..];
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
        var parts = item.Split(';', StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var keyValue = parts[0].Split('=', 2, StringSplitOptions.TrimEntries);
        if (keyValue.Length != 2 || string.IsNullOrWhiteSpace(keyValue[0]) || string.IsNullOrWhiteSpace(keyValue[1]))
        {
            return null;
        }

        return (keyValue[0].Trim(), Uri.UnescapeDataString(keyValue[1].Trim()));
    }

    private static string Truncate(string value) =>
        value.Length <= MaxHeaderLength ? value : value[..MaxHeaderLength];

    private static string? TruncateNullable(string? value) =>
        value is not null && value.Length > MaxHeaderLength ? value[..MaxHeaderLength] : value;
}
